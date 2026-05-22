using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FinanceTracker.Helpers;
using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.ViewModels;

public class BudgetViewModel : ObservableObject
{
    private readonly IFinanceDataService financeDataService;
    private DateTime selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private string displayedMonthLabel = DateTime.Today.ToString("MMMM yyyy");
    private string unallocatedFundsLine = $"{FinanceMath.Currency(0m)} remaining to assign";
    private string poolComment = "// based on income logged in this month";
    private string allocationSummary = "[0 active limits] ▼";

    public BudgetViewModel(IFinanceDataService financeDataService)
    {
        this.financeDataService = financeDataService;
        this.financeDataService.TransactionsChanged += HandleDataChanged;
        this.financeDataService.BudgetsChanged += HandleDataChanged;
        BudgetCategories = [];
        Refresh();
    }

    public ObservableCollection<BudgetCategoryItem> BudgetCategories { get; }

    public DateTime SelectedMonth => selectedMonth;

    public string DisplayedMonthLabel
    {
        get => displayedMonthLabel;
        set => SetProperty(ref displayedMonthLabel, value);
    }

    public string UnallocatedFundsLine
    {
        get => unallocatedFundsLine;
        set => SetProperty(ref unallocatedFundsLine, value);
    }

    public string PoolComment
    {
        get => poolComment;
        set => SetProperty(ref poolComment, value);
    }

    public string AllocationSummary
    {
        get => allocationSummary;
        set => SetProperty(ref allocationSummary, value);
    }

    public void PreviousMonth()
    {
        SelectMonth(selectedMonth.AddMonths(-1));
    }

    public void NextMonth()
    {
        SelectMonth(selectedMonth.AddMonths(1));
    }

    public void SelectMonth(DateTime month)
    {
        selectedMonth = new DateTime(month.Year, month.Month, 1);
        Refresh();
    }

    public async Task SetBudgetAsync(string category, decimal limit)
    {
        var existingBudget = financeDataService.Budgets.FirstOrDefault(item =>
            item.Category == category &&
            item.BudgetMonth == selectedMonth);

        var budget = new BudgetAllocation(
            existingBudget?.Id ?? Guid.NewGuid(),
            category,
            limit,
            selectedMonth,
            DateTime.UtcNow);

        await financeDataService.SetBudgetAsync(budget);
    }

    public async Task ClearBudgetAsync(string category)
    {
        await financeDataService.DeleteBudgetAsync(category, selectedMonth);
    }

    private void HandleDataChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var nextMonth = selectedMonth.AddMonths(1);
        var transactions = financeDataService.Transactions
            .Where(item => item.EntryDate >= selectedMonth && item.EntryDate < nextMonth)
            .ToList();
        var monthBudgets = financeDataService.Budgets
            .Where(item => item.BudgetMonth == selectedMonth)
            .ToDictionary(item => item.Category, item => item);

        BudgetCategories.Clear();

        foreach (var category in FinanceCatalog.GetCategories(FinanceEntryType.Expense))
        {
            var spent = transactions
                .Where(item => item.EntryType == FinanceEntryType.Expense && item.Category == category)
                .Sum(item => item.Amount);
            var limit = monthBudgets.TryGetValue(category, out var budget) ? budget.Limit : 0m;

            BudgetCategories.Add(new BudgetCategoryItem(category, spent, limit));
        }

        var monthIncome = transactions
            .Where(item => item.EntryType == FinanceEntryType.Income)
            .Sum(item => item.Amount);
        var assigned = BudgetCategories.Sum(item => item.Limit);
        var unallocated = monthIncome - assigned;
        var configuredCount = BudgetCategories.Count(item => item.Limit > 0m);

        DisplayedMonthLabel = selectedMonth.ToString("MMMM yyyy");
        UnallocatedFundsLine = FormatPoolLine(unallocated);
        PoolComment = $"// {FinanceMath.Currency(monthIncome)} income logged in {selectedMonth:MMMM yyyy}";
        AllocationSummary = configuredCount == 1
            ? "[1 active limit] ▼"
            : $"[{configuredCount} active limits] ▼";
    }

    private static string FormatPoolLine(decimal unallocated)
    {
        if (unallocated < 0m)
        {
            return $"{FinanceMath.Currency(Math.Abs(unallocated))} over-assigned for this month";
        }

        if (unallocated == 0m)
        {
            return $"{FinanceMath.Currency(0m)} fully allocated";
        }

        return $"{FinanceMath.Currency(unallocated)} remaining to assign";
    }
}

public sealed class BudgetCategoryItem
{
    public BudgetCategoryItem(string category, decimal spent, decimal limit)
    {
        Category = category;
        CategoryKey = $"ops.{category.ToLowerInvariant()}";
        Spent = decimal.Round(spent, 2, MidpointRounding.AwayFromZero);
        Limit = decimal.Round(limit, 2, MidpointRounding.AwayFromZero);
    }

    public string Category { get; }

    public string CategoryKey { get; }

    public decimal Spent { get; }

    public decimal Limit { get; }

    public decimal UsageRatio =>
        Limit <= 0m
            ? Spent > 0m ? 1m : 0m
            : Spent / Limit;

    public int UsagePercent => Limit <= 0m
        ? Spent > 0m ? 100 : 0
        : (int)Math.Round((Spent / Limit) * 100m, MidpointRounding.AwayFromZero);

    public string StatusToken => UsageRatio switch
    {
        >= 1m => "[ERR]",
        >= 0.8m => "[WARN]",
        _ => "[OK]"
    };

    public Color StatusColor => UsageRatio switch
    {
        >= 1m => Color.FromArgb("#E06C75"),
        >= 0.8m => Color.FromArgb("#F5A623"),
        _ => Color.FromArgb("#56B6C2")
    };

    public string StatusAndCategoryLine => $"{StatusToken} {CategoryKey}";

    public string ProgressLine => $"[{BuildProgressBar()}] {UsagePercent}%";

    public string SummaryLine => $"// {FinanceMath.Currency(Spent)} spent of {FinanceMath.Currency(Limit)} limit";

    public string ActionToken => Limit > 0m ? "[ edit ]" : "[ set ]";

    public string ClearToken => "[ clear ]";

    public Color ActionColor => Limit > 0m ? Color.FromArgb("#F5A623") : Color.FromArgb("#56B6C2");

    public Color ClearColor => Color.FromArgb("#6C7A89");

    public bool HasBudget => Limit > 0m;

    private string BuildProgressBar()
    {
        var ratioForBar = Math.Clamp((double)UsageRatio, 0d, 1d);
        var filledSlots = (int)Math.Round(ratioForBar * 10d, MidpointRounding.AwayFromZero);
        filledSlots = Math.Clamp(filledSlots, 0, 10);
        var emptySlots = 10 - filledSlots;
        return $"{new string('█', filledSlots)}{new string('░', emptySlots)}";
    }
}
