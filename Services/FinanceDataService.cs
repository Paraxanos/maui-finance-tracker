using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceTracker.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace FinanceTracker.Services;

public sealed class FinanceDataService : IFinanceDataService
{
    private const string TransactionsStorageFileName = "transactions.json";
    private const string BudgetsStorageFileName = "budgets.json";
    private const string ProfileStorageFileName = "profile.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private List<FinanceRecord> transactions = [];
    private List<BudgetAllocation> budgets = [];
    private UserProfile userProfile = new("User", "user@finance.tracker", []);
    private Guid? selectedBankAccountId;
    private bool isInitialized;

    public IReadOnlyList<FinanceRecord> Transactions => transactions;

    public IReadOnlyList<BudgetAllocation> Budgets => budgets;

    public UserProfile Profile => userProfile;

    public bool IsProfileComplete =>
        userProfile.HasCompletedSetup;

    public Guid? SelectedBankAccountId
    {
        get => selectedBankAccountId;
        set
        {
            if (selectedBankAccountId != value)
            {
                selectedBankAccountId = value;
                RaiseSelectedBankAccountChanged();
            }
        }
    }

    public event EventHandler? TransactionsChanged;

    public event EventHandler? BudgetsChanged;

    public event EventHandler? ProfileChanged;

    public event EventHandler? SelectedBankAccountChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (isInitialized)
        {
            return;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            if (isInitialized)
            {
                return;
            }

            await LoadTransactionsUnsafeAsync(cancellationToken);
            await LoadBudgetsUnsafeAsync(cancellationToken);
            await LoadProfileUnsafeAsync(cancellationToken);

            isInitialized = true;
        }
        finally
        {
            gate.Release();
        }

        RaiseTransactionsChanged();
        RaiseBudgetsChanged();
        RaiseProfileChanged();
    }

    public async Task AddTransactionAsync(FinanceRecord record, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await gate.WaitAsync(cancellationToken);

        try
        {
            transactions.Add(record with { EntryDate = record.EntryDate.Date });
            SortUnsafe();
            await SaveTransactionsUnsafeAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        RaiseTransactionsChanged();
    }

    public async Task UpdateTransactionAsync(FinanceRecord record, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await gate.WaitAsync(cancellationToken);

        try
        {
            var index = transactions.FindIndex(item => item.Id == record.Id);

            if (index < 0)
            {
                return;
            }

            transactions[index] = record with { EntryDate = record.EntryDate.Date };
            SortUnsafe();
            await SaveTransactionsUnsafeAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        RaiseTransactionsChanged();
    }

    public async Task DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await gate.WaitAsync(cancellationToken);

        try
        {
            transactions = transactions.Where(item => item.Id != id).ToList();
            await SaveTransactionsUnsafeAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        RaiseTransactionsChanged();
    }

    public async Task SetBudgetAsync(BudgetAllocation budget, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await gate.WaitAsync(cancellationToken);

        try
        {
            var normalizedMonth = NormalizeBudgetMonth(budget.BudgetMonth);
            var existingIndex = budgets.FindIndex(item =>
                item.Category == budget.Category &&
                item.BudgetMonth == normalizedMonth);

            var normalizedBudget = budget with
            {
                BudgetMonth = normalizedMonth,
                Limit = decimal.Round(budget.Limit, 2, MidpointRounding.AwayFromZero),
                UpdatedAtUtc = DateTime.UtcNow
            };

            if (existingIndex >= 0)
            {
                budgets[existingIndex] = normalizedBudget with { Id = budgets[existingIndex].Id };
            }
            else
            {
                budgets.Add(normalizedBudget);
            }

            SortBudgetsUnsafe();
            await SaveBudgetsUnsafeAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        RaiseBudgetsChanged();
    }

    public async Task DeleteBudgetAsync(string category, DateTime budgetMonth, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await gate.WaitAsync(cancellationToken);

        try
        {
            var normalizedMonth = NormalizeBudgetMonth(budgetMonth);
            budgets = budgets
                .Where(item => !(item.Category == category && item.BudgetMonth == normalizedMonth))
                .ToList();
            await SaveBudgetsUnsafeAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        RaiseBudgetsChanged();
    }

    public async Task SaveProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await gate.WaitAsync(cancellationToken);

        bool transactionsModified = false;
        try
        {
            var currentProfile = NormalizeProfile(userProfile);
            profile = NormalizeProfile(profile);

            // Identify deleted bank accounts
            var existingIds = currentProfile.BankAccounts.Select(a => a.Id).ToHashSet();
            var newIds = profile.BankAccounts.Select(a => a.Id).ToHashSet();
            var deletedIds = existingIds.Where(id => !newIds.Contains(id)).ToList();

            if (deletedIds.Count > 0)
            {
                for (int i = 0; i < transactions.Count; i++)
                {
                    var accountId = transactions[i].BankAccountId;
                    if (accountId is Guid id && deletedIds.Contains(id))
                    {
                        transactions[i] = transactions[i] with { BankAccountId = null };
                        transactionsModified = true;
                    }
                }
            }

            userProfile = profile;
            await SaveProfileUnsafeAsync(cancellationToken);

            if (transactionsModified)
            {
                await SaveTransactionsUnsafeAsync(cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }

        RaiseProfileChanged();
        if (transactionsModified)
        {
            RaiseTransactionsChanged();
        }
    }

    private async Task LoadTransactionsUnsafeAsync(CancellationToken cancellationToken)
    {
        var path = GetTransactionsStoragePath();

        if (File.Exists(path))
        {
            try
            {
                await using var stream = File.OpenRead(path);
                var snapshot = await JsonSerializer.DeserializeAsync<FinanceSnapshot>(stream, JsonOptions, cancellationToken);
                transactions = snapshot?.Transactions?
                    .OrderByDescending(item => item.EntryDate)
                    .ThenByDescending(item => item.CreatedAtUtc)
                    .ToList() ?? [];
            }
            catch (JsonException)
            {
                var backupPath = Path.Combine(
                    FileSystem.AppDataDirectory,
                    $"transactions.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}.json");

                File.Move(path, backupPath, overwrite: true);
                transactions = [];
                await SaveTransactionsUnsafeAsync(cancellationToken);
            }
        }
        else
        {
            transactions = [];
            await SaveTransactionsUnsafeAsync(cancellationToken);
        }
    }

    private async Task LoadBudgetsUnsafeAsync(CancellationToken cancellationToken)
    {
        var path = GetBudgetsStoragePath();

        if (File.Exists(path))
        {
            try
            {
                await using var stream = File.OpenRead(path);
                var snapshot = await JsonSerializer.DeserializeAsync<BudgetSnapshot>(stream, JsonOptions, cancellationToken);
                budgets = snapshot?.Budgets?
                    .Select(item => item with { BudgetMonth = NormalizeBudgetMonth(item.BudgetMonth) })
                    .OrderByDescending(item => item.BudgetMonth)
                    .ThenBy(item => item.Category)
                    .ToList() ?? [];
            }
            catch (JsonException)
            {
                var backupPath = Path.Combine(
                    FileSystem.AppDataDirectory,
                    $"budgets.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}.json");

                File.Move(path, backupPath, overwrite: true);
                budgets = [];
                await SaveBudgetsUnsafeAsync(cancellationToken);
            }
        }
        else
        {
            budgets = [];
            await SaveBudgetsUnsafeAsync(cancellationToken);
        }
    }

    private async Task SaveTransactionsUnsafeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);

        await using var stream = File.Create(GetTransactionsStoragePath());
        var snapshot = new FinanceSnapshot { Transactions = transactions };
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);
    }

    private async Task SaveBudgetsUnsafeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);

        await using var stream = File.Create(GetBudgetsStoragePath());
        var snapshot = new BudgetSnapshot { Budgets = budgets };
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);
    }

    private string GetTransactionsStoragePath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, TransactionsStorageFileName);
    }

    private string GetBudgetsStoragePath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, BudgetsStorageFileName);
    }

    private void SortUnsafe()
    {
        transactions = transactions
            .OrderByDescending(item => item.EntryDate)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ToList();
    }

    private void SortBudgetsUnsafe()
    {
        budgets = budgets
            .OrderByDescending(item => item.BudgetMonth)
            .ThenBy(item => item.Category)
            .ToList();
    }

    private static DateTime NormalizeBudgetMonth(DateTime budgetMonth)
    {
        return new DateTime(budgetMonth.Year, budgetMonth.Month, 1);
    }

    private void RaiseTransactionsChanged()
    {
        if (MainThread.IsMainThread)
        {
            TransactionsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => TransactionsChanged?.Invoke(this, EventArgs.Empty));
    }

    private void RaiseBudgetsChanged()
    {
        if (MainThread.IsMainThread)
        {
            BudgetsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => BudgetsChanged?.Invoke(this, EventArgs.Empty));
    }

    private async Task LoadProfileUnsafeAsync(CancellationToken cancellationToken)
    {
        var path = GetProfileStoragePath();

        if (File.Exists(path))
        {
            try
            {
                await using var stream = File.OpenRead(path);
                userProfile = NormalizeProfile(
                    await JsonSerializer.DeserializeAsync<UserProfile>(stream, JsonOptions, cancellationToken)
                    ?? new UserProfile("User", "user@finance.tracker", []));
            }
            catch (JsonException)
            {
                userProfile = new UserProfile("User", "user@finance.tracker", []);
                await SaveProfileUnsafeAsync(cancellationToken);
            }
        }
        else
        {
            userProfile = new UserProfile("User", "user@finance.tracker", []);
            await SaveProfileUnsafeAsync(cancellationToken);
        }
    }

    private async Task SaveProfileUnsafeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);

        await using var stream = File.Create(GetProfileStoragePath());
        await JsonSerializer.SerializeAsync(stream, userProfile, JsonOptions, cancellationToken);
    }

    private string GetProfileStoragePath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, ProfileStorageFileName);
    }

    private void RaiseProfileChanged()
    {
        if (MainThread.IsMainThread)
        {
            ProfileChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
    }

    private void RaiseSelectedBankAccountChanged()
    {
        if (MainThread.IsMainThread)
        {
            SelectedBankAccountChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => SelectedBankAccountChanged?.Invoke(this, EventArgs.Empty));
    }

    private static UserProfile NormalizeProfile(UserProfile profile)
    {
        return profile with
        {
            BankAccounts = profile.BankAccounts ?? []
        };
    }

    private sealed class FinanceSnapshot
    {
        public List<FinanceRecord> Transactions { get; init; } = [];
    }

    private sealed class BudgetSnapshot
    {
        public List<BudgetAllocation> Budgets { get; init; } = [];
    }
}
