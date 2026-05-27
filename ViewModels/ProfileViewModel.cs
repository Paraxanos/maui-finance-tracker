using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Helpers;
using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.ViewModels;

public sealed partial class ProfileViewModel : ObservableObject
{
    private readonly IFinanceDataService financeDataService;
    private string name = string.Empty;
    private string email = string.Empty;
    private string saveMessage = "ready";
    private Color saveMessageColor = Color.FromArgb("#56B6C2");

    private string accountName = string.Empty;
    private string initialBalanceText = string.Empty;
    private string formTitle = "ADD BANK ACCOUNT";
    private string formButtonText = "[ add account ]";
    private bool isEditing;
    private Guid? editingAccountId;
    private string accountMessage = "ready";
    private Color accountMessageColor = Color.FromArgb("#56B6C2");

    public ProfileViewModel(IFinanceDataService financeDataService)
    {
        this.financeDataService = financeDataService;
        this.financeDataService.TransactionsChanged += HandleTransactionsChanged;
        this.financeDataService.ProfileChanged += HandleProfileChanged;
        Refresh();
    }

    public ObservableCollection<ProfileBankAccountItem> BankAccounts { get; } = [];

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public string SaveMessage
    {
        get => saveMessage;
        set => SetProperty(ref saveMessage, value);
    }

    public Color SaveMessageColor
    {
        get => saveMessageColor;
        set => SetProperty(ref saveMessageColor, value);
    }

    public string AccountName
    {
        get => accountName;
        set => SetProperty(ref accountName, value);
    }

    public string InitialBalanceText
    {
        get => initialBalanceText;
        set => SetProperty(ref initialBalanceText, value);
    }

    public string FormTitle
    {
        get => formTitle;
        set => SetProperty(ref formTitle, value);
    }

    public string FormButtonText
    {
        get => formButtonText;
        set => SetProperty(ref formButtonText, value);
    }

    public bool IsEditing
    {
        get => isEditing;
        set => SetProperty(ref isEditing, value);
    }

    public string AccountMessage
    {
        get => accountMessage;
        set => SetProperty(ref accountMessage, value);
    }

    public Color AccountMessageColor
    {
        get => accountMessageColor;
        set => SetProperty(ref accountMessageColor, value);
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            SetSaveState("name is required", "#E06C75");
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            SetSaveState("email is required", "#E06C75");
            return;
        }

        var currentProfile = financeDataService.Profile;
        var updated = currentProfile with
        {
            Name = Name.Trim(),
            Email = Email.Trim()
        };

        await financeDataService.SaveProfileAsync(updated);
        SetSaveState("profile details saved successfully", "#56B6C2");
    }

    [RelayCommand]
    private async Task AddOrUpdateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(AccountName))
        {
            SetAccountState("account name is required", "#E06C75");
            return;
        }

        if (!decimal.TryParse(InitialBalanceText, out var initialBalance) || initialBalance < 0)
        {
            SetAccountState("initial balance must be a non-negative number", "#E06C75");
            return;
        }

        var currentProfile = financeDataService.Profile;
        var accounts = (currentProfile.BankAccounts ?? []).ToList();

        if (IsEditing && editingAccountId.HasValue)
        {
            var idx = accounts.FindIndex(a => a.Id == editingAccountId.Value);
            if (idx >= 0)
            {
                accounts[idx] = accounts[idx] with
                {
                    Name = AccountName.Trim(),
                    InitialBalance = decimal.Round(initialBalance, 2, MidpointRounding.AwayFromZero)
                };
                SetAccountState("account updated successfully", "#56B6C2");
            }
        }
        else
        {
            // Check for duplicate names
            if (accounts.Any(a => string.Equals(a.Name, AccountName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                SetAccountState("an account with this name already exists", "#E06C75");
                return;
            }

            var newAccount = new BankAccount(
                Guid.NewGuid(),
                AccountName.Trim(),
                decimal.Round(initialBalance, 2, MidpointRounding.AwayFromZero),
                DateTime.UtcNow);

            accounts.Add(newAccount);
            SetAccountState("account created successfully", "#56B6C2");
        }

        var updated = currentProfile with { BankAccounts = accounts };
        await financeDataService.SaveProfileAsync(updated);

        CancelEdit();
    }

    [RelayCommand]
    private void EditAccount(ProfileBankAccountItem item)
    {
        if (item is null)
        {
            return;
        }

        AccountName = item.Name;
        InitialBalanceText = item.InitialBalance.ToString("F2");
        IsEditing = true;
        editingAccountId = item.Id;
        FormTitle = "EDIT BANK ACCOUNT";
        FormButtonText = "[ update account ]";
        SetAccountState($"editing {item.Name}", "#56B6C2");
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(ProfileBankAccountItem item)
    {
        if (item is null)
        {
            return;
        }

        var currentProfile = financeDataService.Profile;
        var accounts = (currentProfile.BankAccounts ?? []).Where(a => a.Id != item.Id).ToList();
        var updated = currentProfile with { BankAccounts = accounts };

        await financeDataService.SaveProfileAsync(updated);
        SetAccountState($"deleted {item.Name}", "#E06C75");

        if (IsEditing && editingAccountId == item.Id)
        {
            CancelEdit();
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        AccountName = string.Empty;
        InitialBalanceText = string.Empty;
        IsEditing = false;
        editingAccountId = null;
        FormTitle = "ADD BANK ACCOUNT";
        FormButtonText = "[ add account ]";
    }

    private void HandleTransactionsChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void HandleProfileChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var profile = financeDataService.Profile;
        Name = profile.Name;
        Email = profile.Email;

        BankAccounts.Clear();
        foreach (var account in profile.BankAccounts ?? [])
        {
            var net = financeDataService.Transactions
                .Where(t => t.BankAccountId == account.Id)
                .Sum(t => t.EntryType == FinanceEntryType.Income ? t.Amount : -t.Amount);

            var currentBalance = account.InitialBalance + net;
            BankAccounts.Add(new ProfileBankAccountItem(account.Id, account.Name, account.InitialBalance, currentBalance));
        }
    }

    private void SetSaveState(string message, string colorHex)
    {
        SaveMessage = message;
        SaveMessageColor = Color.FromArgb(colorHex);
    }

    private void SetAccountState(string message, string colorHex)
    {
        AccountMessage = message;
        AccountMessageColor = Color.FromArgb(colorHex);
    }
}

public sealed class ProfileBankAccountItem
{
    public Guid Id { get; }
    public string Name { get; }
    public decimal InitialBalance { get; }
    public string InitialBalanceLabel => $"// initial: {FinanceMath.Currency(InitialBalance)}";
    public decimal CurrentBalance { get; }
    public string CurrentBalanceLabel => FinanceMath.Currency(CurrentBalance);
    public string TitleLine => $"💳 {Name}";

    public ProfileBankAccountItem(Guid id, string name, decimal initialBalance, decimal currentBalance)
    {
        Id = id;
        Name = name;
        InitialBalance = initialBalance;
        CurrentBalance = currentBalance;
    }
}
