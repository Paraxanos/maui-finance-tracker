using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Helpers;
using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.ViewModels;

public partial class AddExpenseViewModel : ObservableObject
{
    private readonly IFinanceDataService financeDataService;
    private string selectedEntryType = "Expense";
    private string selectedCategory = "Food";
    private string title = string.Empty;
    private string amountText = string.Empty;
    private string note = string.Empty;
    private DateTime selectedDate = DateTime.Today;
    private bool isCleared = true;
    private string formMessage = "ready for a new log entry";
    private Color formMessageColor = Color.FromArgb("#56B6C2");

    public IReadOnlyList<string> EntryTypes { get; } = ["Expense", "Income"];

    public ObservableCollection<string> AvailableCategories { get; } = [];

    public AddExpenseViewModel(IFinanceDataService financeDataService)
    {
        this.financeDataService = financeDataService;
        LoadCategories();
    }

    public string SelectedEntryType
    {
        get => selectedEntryType;
        set
        {
            if (!SetProperty(ref selectedEntryType, value))
            {
                return;
            }

            LoadCategories();
            SetFormState(value == "Income" ? "logging inflow" : "logging expense", "#F5A623");
        }
    }

    public string SelectedCategory
    {
        get => selectedCategory;
        set => SetProperty(ref selectedCategory, value);
    }

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public string AmountText
    {
        get => amountText;
        set => SetProperty(ref amountText, value);
    }

    public string Note
    {
        get => note;
        set => SetProperty(ref note, value);
    }

    public DateTime SelectedDate
    {
        get => selectedDate;
        set => SetProperty(ref selectedDate, value);
    }

    public bool IsCleared
    {
        get => isCleared;
        set
        {
            if (!SetProperty(ref isCleared, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ClearedTokenLabel));
        }
    }

    public string FormMessage
    {
        get => formMessage;
        set => SetProperty(ref formMessage, value);
    }

    public Color FormMessageColor
    {
        get => formMessageColor;
        set => SetProperty(ref formMessageColor, value);
    }

    public string ClearedTokenLabel => IsCleared ? "[x] cleared" : "[ ] cleared";

    [RelayCommand]
    private void ToggleCleared()
    {
        IsCleared = !IsCleared;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            SetFormState("title is required", "#E06C75");
            return;
        }

        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            SetFormState("amount must be a positive number", "#E06C75");
            return;
        }

        var entryType = SelectedEntryType == "Income"
            ? FinanceEntryType.Income
            : FinanceEntryType.Expense;

        var record = new FinanceRecord(
            Guid.NewGuid(),
            Title.Trim(),
            SelectedCategory,
            Note.Trim(),
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            entryType,
            SelectedDate.Date,
            IsCleared,
            DateTime.UtcNow);

        await financeDataService.AddTransactionAsync(record);

        Title = string.Empty;
        AmountText = string.Empty;
        Note = string.Empty;
        SelectedDate = DateTime.Today;
        IsCleared = true;

        SetFormState("entry saved locally on this device", "#56B6C2");
    }

    private void LoadCategories()
    {
        var categories = FinanceCatalog.GetCategories(
            SelectedEntryType == "Income" ? FinanceEntryType.Income : FinanceEntryType.Expense);

        AvailableCategories.Clear();

        foreach (var category in categories)
        {
            AvailableCategories.Add(category);
        }

        SelectedCategory = AvailableCategories.FirstOrDefault() ?? string.Empty;
    }

    private void SetFormState(string message, string colorHex)
    {
        FormMessage = message;
        FormMessageColor = Color.FromArgb(colorHex);
    }
}
