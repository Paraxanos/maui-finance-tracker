using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FinanceTracker.Helpers;
using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.ViewModels;

public class HistoryViewModel : ObservableObject
{
    private readonly IFinanceDataService financeDataService;
    private string ledgerSummary = "0 entries stored";
    private string lifetimeNetLabel = FinanceMath.SignedCurrency(0m);
    private bool isEmptyStateVisible = true;
    private bool hasTransactions;

    public HistoryViewModel(IFinanceDataService financeDataService)
    {
        this.financeDataService = financeDataService;
        this.financeDataService.TransactionsChanged += HandleTransactionsChanged;
        Refresh();
    }

    public ObservableCollection<HistoryMonthGroup> MonthGroups { get; } = [];

    public string LedgerSummary
    {
        get => ledgerSummary;
        set => SetProperty(ref ledgerSummary, value);
    }

    public string LifetimeNetLabel
    {
        get => lifetimeNetLabel;
        set => SetProperty(ref lifetimeNetLabel, value);
    }

    public bool IsEmptyStateVisible
    {
        get => isEmptyStateVisible;
        set => SetProperty(ref isEmptyStateVisible, value);
    }

    public bool HasTransactions
    {
        get => hasTransactions;
        set => SetProperty(ref hasTransactions, value);
    }

    public async Task UpdateTransactionAsync(FinanceRecord record)
    {
        await financeDataService.UpdateTransactionAsync(record);
    }

    public async Task DeleteTransactionAsync(Guid id)
    {
        await financeDataService.DeleteTransactionAsync(id);
    }

    private void HandleTransactionsChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var transactions = financeDataService.Transactions.ToList();

        MonthGroups.Clear();

        foreach (var group in transactions
                     .GroupBy(item => new DateTime(item.EntryDate.Year, item.EntryDate.Month, 1))
                     .OrderByDescending(group => group.Key))
        {
            var items = group
                .OrderByDescending(item => item.EntryDate)
                .ThenByDescending(item => item.CreatedAtUtc)
                .Select(item => new HistoryTransactionItem(item))
                .ToList();

            MonthGroups.Add(new HistoryMonthGroup(group.Key, items));
        }

        LedgerSummary = transactions.Count == 0
            ? "0 entries stored"
            : $"{transactions.Count} entries stored across {MonthGroups.Count} months";
        LifetimeNetLabel = FinanceMath.SignedCurrency(FinanceMath.Net(transactions));
        IsEmptyStateVisible = transactions.Count == 0;
        HasTransactions = transactions.Count > 0;
    }
}

public sealed class HistoryMonthGroup : ObservableCollection<HistoryTransactionItem>
{
    public HistoryMonthGroup(DateTime month, IReadOnlyList<HistoryTransactionItem> entries)
        : base(entries)
    {
        HeaderLine = $"📁 {month:MMMM yyyy} // {entries.Count} {(entries.Count == 1 ? "entry" : "entries")}";
        var net = entries.Sum(item => item.SignedAmount);
        MetaLine = $"// net {FinanceMath.SignedCurrency(net)}";
    }

    public string HeaderLine { get; }

    public string MetaLine { get; }
}

public sealed class HistoryTransactionItem
{
    public HistoryTransactionItem(FinanceRecord record)
    {
        Source = record;
        CheckToken = record.IsCleared ? "[x]" : "[ ]";
        TitleLine = $"{FinanceCatalog.ResolveCategoryIcon(record.Category)} {record.Title}";
        MetaLine = $"// {record.Category} | {record.EntryDate:dd MMM}";
        EditToken = "[ edit ]";
        DeleteToken = "[ delete ]";
        SignedAmount = FinanceMath.SignedAmount(record);
        AmountLabel = FinanceMath.SignedCurrency(SignedAmount);
        AmountColor = SignedAmount >= 0
            ? Color.FromArgb("#56B6C2")
            : Color.FromArgb("#E06C75");
        EditColor = Color.FromArgb("#56B6C2");
        DeleteColor = Color.FromArgb("#E06C75");
    }

    public FinanceRecord Source { get; }

    public Guid Id => Source.Id;

    public string CheckToken { get; }

    public string TitleLine { get; }

    public string MetaLine { get; }

    public string EditToken { get; }

    public string DeleteToken { get; }

    public decimal SignedAmount { get; }

    public string AmountLabel { get; }

    public Color AmountColor { get; }

    public Color EditColor { get; }

    public Color DeleteColor { get; }
}
