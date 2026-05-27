using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FinanceTracker.Helpers;
using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.ViewModels;

public class OverviewViewModel : ObservableObject
{
    private readonly IFinanceDataService financeDataService;
    private DateTime selectedDate = DateTime.Today;
    private DateTime displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private string currentBalanceLabel = FinanceMath.SignedCurrency(0m);
    private string totalIncomeLabel = FinanceMath.Currency(0m);
    private string totalExpenseLabel = FinanceMath.Currency(0m);
    private string thisMonthSpentLabel = FinanceMath.Currency(0m);
    private string thisMonthNetLabel = FinanceMath.SignedCurrency(0m);
    private string todaySpendLabel = FinanceMath.Currency(0m);
    private string transactionCountLabel = "[0 entries] ▼";
    private string filteredEntryCountLabel = "[0 entries] ▼";
    private string headerMeta = "// track every penny. zero-based budgeting.";
    private string ledgerComment = "// showing today's transactions";
    private string displayedMonthLabel = DateTime.Today.ToString("MMMM yyyy");
    private BankAccountSelectionItem? selectedBankAccount;

    public OverviewViewModel(IFinanceDataService financeDataService)
    {
        this.financeDataService = financeDataService;
        this.financeDataService.TransactionsChanged += HandleTransactionsChanged;
        this.financeDataService.ProfileChanged += HandleProfileChanged;
        this.financeDataService.SelectedBankAccountChanged += HandleSelectedBankAccountChanged;
        CalendarDays = [];
        RecentTransactions = [];
        DailySpendTrend = [];
        CategoryTrend = [];
        MonthlyNetTrend = [];
        LedgerStatusTrend = [];
        UpdateBankAccountsList();
        BuildMonthCalendar();
        Refresh();
    }

    public ObservableCollection<MonthCalendarDayItem> CalendarDays { get; }

    public ObservableCollection<LedgerTransactionItem> RecentTransactions { get; }

    public ObservableCollection<AsciiChartItem> DailySpendTrend { get; }

    public ObservableCollection<AsciiChartItem> CategoryTrend { get; }

    public ObservableCollection<AsciiChartItem> MonthlyNetTrend { get; }

    public ObservableCollection<AsciiChartItem> LedgerStatusTrend { get; }

    public string CurrentBalanceLabel
    {
        get => currentBalanceLabel;
        set => SetProperty(ref currentBalanceLabel, value);
    }

    public ObservableCollection<BankAccountSelectionItem> BankAccountsList { get; } = [];

    public BankAccountSelectionItem? SelectedBankAccount
    {
        get => selectedBankAccount;
        set
        {
            if (SetProperty(ref selectedBankAccount, value))
            {
                if (financeDataService.SelectedBankAccountId != value?.Id)
                {
                    financeDataService.SelectedBankAccountId = value?.Id;
                }
            }
        }
    }

    public string TotalIncomeLabel
    {
        get => totalIncomeLabel;
        set => SetProperty(ref totalIncomeLabel, value);
    }

    public string TotalExpenseLabel
    {
        get => totalExpenseLabel;
        set => SetProperty(ref totalExpenseLabel, value);
    }

    public string ThisMonthSpentLabel
    {
        get => thisMonthSpentLabel;
        set => SetProperty(ref thisMonthSpentLabel, value);
    }

    public string ThisMonthNetLabel
    {
        get => thisMonthNetLabel;
        set => SetProperty(ref thisMonthNetLabel, value);
    }

    public string TodaySpendLabel
    {
        get => todaySpendLabel;
        set => SetProperty(ref todaySpendLabel, value);
    }

    public string TransactionCountLabel
    {
        get => transactionCountLabel;
        set => SetProperty(ref transactionCountLabel, value);
    }

    public string FilteredEntryCountLabel
    {
        get => filteredEntryCountLabel;
        set => SetProperty(ref filteredEntryCountLabel, value);
    }

    public string HeaderMeta
    {
        get => headerMeta;
        set => SetProperty(ref headerMeta, value);
    }

    public string LedgerComment
    {
        get => ledgerComment;
        set => SetProperty(ref ledgerComment, value);
    }

    public string DisplayedMonthLabel
    {
        get => displayedMonthLabel;
        set => SetProperty(ref displayedMonthLabel, value);
    }

    public void SelectDate(DateTime date)
    {
        selectedDate = date.Date;
        displayedMonth = new DateTime(date.Year, date.Month, 1);
        BuildMonthCalendar();
        Refresh();
    }

    public void PreviousMonth()
    {
        displayedMonth = displayedMonth.AddMonths(-1);
        selectedDate = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        BuildMonthCalendar();
        Refresh();
    }

    public void NextMonth()
    {
        displayedMonth = displayedMonth.AddMonths(1);
        selectedDate = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        BuildMonthCalendar();
        Refresh();
    }

    private void HandleTransactionsChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void HandleProfileChanged(object? sender, EventArgs e)
    {
        UpdateBankAccountsList();
        Refresh();
    }

    private void HandleSelectedBankAccountChanged(object? sender, EventArgs e)
    {
        var targetId = financeDataService.SelectedBankAccountId;
        var matched = BankAccountsList.FirstOrDefault(item => item.Id == targetId);
        if (matched != null && selectedBankAccount != matched)
        {
            SetProperty(ref selectedBankAccount, matched, nameof(SelectedBankAccount));
        }
        Refresh();
    }

    private void UpdateBankAccountsList()
    {
        var currentSelectedId = financeDataService.SelectedBankAccountId;
        BankAccountsList.Clear();
        BankAccountsList.Add(new BankAccountSelectionItem(null, "All Accounts"));
        foreach (var account in financeDataService.Profile.BankAccounts)
        {
            BankAccountsList.Add(new BankAccountSelectionItem(account.Id, account.Name));
        }
        
        var target = BankAccountsList.FirstOrDefault(item => item.Id == currentSelectedId) 
            ?? BankAccountsList.First();
        
        if (selectedBankAccount != target)
        {
            SetProperty(ref selectedBankAccount, target, nameof(SelectedBankAccount));
        }
    }

    private void Refresh()
    {
        var allTransactions = financeDataService.Transactions.ToList();
        var selectedAccountId = financeDataService.SelectedBankAccountId;
        var transactions = selectedAccountId.HasValue
            ? allTransactions.Where(item => item.BankAccountId == selectedAccountId.Value).ToList()
            : allTransactions;

        var monthStart = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var monthTransactions = transactions
            .Where(item => item.EntryDate >= monthStart && item.EntryDate < nextMonth)
            .ToList();

        var todayTransactions = transactions
            .Where(item => item.EntryDate == DateTime.Today && item.EntryType == FinanceEntryType.Expense)
            .ToList();

        var filteredTransactions = transactions
            .Where(item => item.EntryDate == selectedDate)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToList();

        CurrentBalanceLabel = FinanceMath.SignedCurrency(FinanceMath.Net(transactions));
        TotalIncomeLabel = FinanceMath.Currency(FinanceMath.TotalIncome(transactions));
        TotalExpenseLabel = FinanceMath.Currency(FinanceMath.TotalExpenses(transactions));
        ThisMonthSpentLabel = FinanceMath.Currency(FinanceMath.TotalExpenses(monthTransactions));
        ThisMonthNetLabel = FinanceMath.SignedCurrency(FinanceMath.Net(monthTransactions));
        TodaySpendLabel = FinanceMath.Currency(FinanceMath.TotalExpenses(todayTransactions));
        TransactionCountLabel = FormatCount(transactions.Count);
        FilteredEntryCountLabel = FormatCount(filteredTransactions.Count);
        HeaderMeta = $"// {selectedDate:dddd, d MMMM yyyy}";
        LedgerComment = selectedDate == DateTime.Today
            ? "// showing today's transactions"
            : $"// showing transactions for {selectedDate:dd MMM}";
        DisplayedMonthLabel = displayedMonth.ToString("MMMM yyyy");

        BuildRecentTransactions(filteredTransactions);
        BuildDailySpendTrend(transactions);
        BuildCategoryTrend(monthTransactions);
        BuildMonthlyNetTrend(transactions);
        BuildLedgerStatusTrend(monthTransactions);
    }

    private void BuildMonthCalendar()
    {
        CalendarDays.Clear();

        var firstDay = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        var offset = ((int)firstDay.DayOfWeek + 6) % 7;
        var daysInMonth = DateTime.DaysInMonth(displayedMonth.Year, displayedMonth.Month);
        var totalCells = (int)Math.Ceiling((offset + daysInMonth) / 7d) * 7;

        for (var cell = 0; cell < totalCells; cell++)
        {
            var dayNumber = cell - offset + 1;

            if (dayNumber < 1 || dayNumber > daysInMonth)
            {
                CalendarDays.Add(MonthCalendarDayItem.Placeholder());
                continue;
            }

            var date = new DateTime(displayedMonth.Year, displayedMonth.Month, dayNumber);
            CalendarDays.Add(new MonthCalendarDayItem(
                date,
                date == selectedDate,
                date == DateTime.Today));
        }
    }

    private void BuildRecentTransactions(List<FinanceRecord> transactions)
    {
        RecentTransactions.Clear();

        foreach (var transaction in transactions)
        {
            RecentTransactions.Add(new LedgerTransactionItem(transaction));
        }
    }

    private void BuildDailySpendTrend(List<FinanceRecord> transactions)
    {
        DailySpendTrend.Clear();

        var start = selectedDate.AddDays(-6);
        var values = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var day = start.AddDays(offset);
                var total = transactions
                    .Where(item => item.EntryDate == day && item.EntryType == FinanceEntryType.Expense)
                    .Sum(item => item.Amount);
                return new { Day = day, Total = total };
            })
            .ToList();

        var max = values.Max(item => item.Total);

        foreach (var item in values)
        {
            DailySpendTrend.Add(new AsciiChartItem(
                item.Day.ToString("dd MMM"),
                BuildBar(item.Total, max),
                FinanceMath.Currency(item.Total),
                Color.FromArgb("#F5A623"),
                Color.FromArgb("#F5A623")));
        }
    }

    private void BuildCategoryTrend(List<FinanceRecord> monthTransactions)
    {
        CategoryTrend.Clear();

        var groups = monthTransactions
            .Where(item => item.EntryType == FinanceEntryType.Expense)
            .GroupBy(item => item.Category)
            .Select(group => new
            {
                Category = group.Key,
                Total = group.Sum(item => item.Amount)
            })
            .OrderByDescending(group => group.Total)
            .Take(5)
            .ToList();

        var max = groups.Count == 0 ? 0 : groups.Max(item => item.Total);

        foreach (var group in groups)
        {
            CategoryTrend.Add(new AsciiChartItem(
                group.Category,
                BuildBar(group.Total, max),
                FinanceMath.Currency(group.Total),
                Color.FromArgb("#56B6C2"),
                Color.FromArgb("#56B6C2")));
        }
    }

    private void BuildMonthlyNetTrend(List<FinanceRecord> transactions)
    {
        MonthlyNetTrend.Clear();

        var monthStarts = Enumerable.Range(0, 6)
            .Select(offset => displayedMonth.AddMonths(offset - 5))
            .ToList();

        var values = monthStarts
            .Select(monthStart =>
            {
                var nextMonth = monthStart.AddMonths(1);
                var records = transactions
                    .Where(item => item.EntryDate >= monthStart && item.EntryDate < nextMonth)
                    .ToList();

                return new
                {
                    Month = monthStart,
                    Net = FinanceMath.Net(records)
                };
            })
            .ToList();

        var maxMagnitude = values.Count == 0 ? 0m : values.Max(item => Math.Abs(item.Net));

        foreach (var item in values)
        {
            var color = item.Net >= 0 ? Color.FromArgb("#56B6C2") : Color.FromArgb("#E06C75");
            MonthlyNetTrend.Add(new AsciiChartItem(
                item.Month.ToString("MMM yy"),
                BuildBar(Math.Abs(item.Net), maxMagnitude),
                FinanceMath.SignedCurrency(item.Net),
                color,
                color));
        }
    }

    private void BuildLedgerStatusTrend(List<FinanceRecord> monthTransactions)
    {
        LedgerStatusTrend.Clear();

        var clearedCount = monthTransactions.Count(item => item.IsCleared);
        var pendingCount = monthTransactions.Count - clearedCount;
        var max = Math.Max(clearedCount, pendingCount);

        LedgerStatusTrend.Add(new AsciiChartItem(
            "cleared",
            BuildBar(clearedCount, max, '='),
            clearedCount == 1 ? "1 entry" : $"{clearedCount} entries",
            Color.FromArgb("#56B6C2"),
            Color.FromArgb("#56B6C2")));

        LedgerStatusTrend.Add(new AsciiChartItem(
            "pending",
            BuildBar(pendingCount, max, '='),
            pendingCount == 1 ? "1 entry" : $"{pendingCount} entries",
            Color.FromArgb("#F5A623"),
            Color.FromArgb("#F5A623")));
    }

    private static string BuildBar(decimal value, decimal max, char fill = '#')
    {
        if (value <= 0 || max <= 0)
        {
            return ".";
        }

        var length = Math.Max(1, (int)Math.Round((double)(value / max) * 12));
        return new string(fill, length);
    }

    private static string FormatCount(int count)
    {
        return count == 1 ? "[1 entry] ▼" : $"[{count} entries] ▼";
    }
}

public sealed class MonthCalendarDayItem
{
    public MonthCalendarDayItem(DateTime date, bool isSelected, bool isToday)
    {
        Date = date;
        DayText = date.Day.ToString();
        IsSelectable = true;
        BackgroundColor = isSelected ? Color.FromArgb("#F5A623") : Colors.Transparent;
        TextColor = isSelected
            ? Color.FromArgb("#0A0E17")
            : isToday
                ? Color.FromArgb("#56B6C2")
                : Color.FromArgb("#E6E6E6");
    }

    private MonthCalendarDayItem()
    {
        DayText = string.Empty;
        IsSelectable = false;
        BackgroundColor = Colors.Transparent;
        TextColor = Colors.Transparent;
    }

    public DateTime Date { get; }

    public string DayText { get; }

    public bool IsSelectable { get; }

    public Color BackgroundColor { get; }

    public Color TextColor { get; }

    public static MonthCalendarDayItem Placeholder() => new();
}

public sealed class LedgerTransactionItem
{
    public LedgerTransactionItem(FinanceRecord record)
    {
        Date = record.EntryDate;
        CheckToken = record.IsCleared ? "[x]" : "[ ]";
        TitleLine = $"{FinanceCatalog.ResolveCategoryIcon(record.Category)} {record.Title}";
        MetaLine = $"// {record.Category} | {record.EntryDate:dd MMM}";

        var signedAmount = FinanceMath.SignedAmount(record);
        AmountLabel = FinanceMath.SignedCurrency(signedAmount);
        AmountColor = signedAmount >= 0
            ? Color.FromArgb("#56B6C2")
            : Color.FromArgb("#E06C75");
    }

    public DateTime Date { get; }

    public string CheckToken { get; }

    public string TitleLine { get; }

    public string MetaLine { get; }

    public string AmountLabel { get; }

    public Color AmountColor { get; }
}

public sealed class AsciiChartItem
{
    public AsciiChartItem(string label, string barText, string amountLabel, Color amountColor, Color barColor)
    {
        Label = label;
        BarText = barText;
        AmountLabel = amountLabel;
        AmountColor = amountColor;
        BarColor = barColor;
    }

    public string Label { get; }

    public string BarText { get; }

    public string AmountLabel { get; }

    public Color AmountColor { get; }

    public Color BarColor { get; }
}
