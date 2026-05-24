using FinanceTracker.Helpers;
using FinanceTracker.Models;
using FinanceTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Pages;

public partial class HistoryPage : ContentPage
{
    private HistoryViewModel ViewModel =>
        (HistoryViewModel)(BindingContext ?? throw new InvalidOperationException("Missing view model."));

    public HistoryPage()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<HistoryViewModel>();
        FinanceTracker.Helpers.SwipeNavigationHelper.AddSwipeGestures(this, "budget", "overview");
    }

    private async void OnEditTapped(object? sender, TappedEventArgs e)
    {
        if (ResolveHistoryTransactionItem(sender, e) is not { } item)
        {
            return;
        }

        var record = item.Source;

        var title = await DisplayPromptAsync(
            "Edit Entry",
            "Title",
            initialValue: record.Title);

        if (title is null)
        {
            return;
        }

        title = title.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlertAsync("Edit Entry", "Title cannot be empty.", "OK");
            return;
        }

        var amountText = await DisplayPromptAsync(
            "Edit Entry",
            "Amount",
            keyboard: Keyboard.Numeric,
            initialValue: record.Amount.ToString("0.##"));

        if (amountText is null)
        {
            return;
        }

        if (!decimal.TryParse(amountText, out var amount) || amount <= 0)
        {
            await DisplayAlertAsync("Edit Entry", "Amount must be a positive number.", "OK");
            return;
        }

        var note = await DisplayPromptAsync(
            "Edit Entry",
            "Note",
            initialValue: record.Note);

        if (note is null)
        {
            return;
        }

        var dateText = await DisplayPromptAsync(
            "Edit Entry",
            "Date (yyyy-MM-dd)",
            initialValue: record.EntryDate.ToString("yyyy-MM-dd"));

        if (dateText is null)
        {
            return;
        }

        if (!DateTime.TryParse(dateText, out var entryDate))
        {
            await DisplayAlertAsync("Edit Entry", "Use a valid date such as 2026-05-22.", "OK");
            return;
        }

        var entryTypeChoice = await DisplayActionSheetAsync(
            "Entry Type",
            "Cancel",
            null,
            "Expense",
            "Income");

        if (entryTypeChoice == "Cancel")
        {
            return;
        }

        var categories = FinanceCatalog.GetCategories(
            entryTypeChoice == "Income" ? FinanceEntryType.Income : FinanceEntryType.Expense);

        var categoryChoice = await DisplayActionSheetAsync(
            "Category",
            "Cancel",
            null,
            [.. categories]);

        if (categoryChoice == "Cancel")
        {
            return;
        }

        var statusChoice = await DisplayActionSheetAsync(
            "Status",
            "Cancel",
            null,
            "Cleared",
            "Pending");

        if (statusChoice == "Cancel")
        {
            return;
        }

        var updated = record with
        {
            Title = title,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            Note = note.Trim(),
            EntryDate = entryDate.Date,
            EntryType = entryTypeChoice == "Income" ? FinanceEntryType.Income : FinanceEntryType.Expense,
            Category = categoryChoice,
            IsCleared = statusChoice == "Cleared"
        };

        await ViewModel.UpdateTransactionAsync(updated);
    }

    private async void OnDeleteTapped(object? sender, TappedEventArgs e)
    {
        if (ResolveHistoryTransactionItem(sender, e) is not { } item)
        {
            return;
        }

        var shouldDelete = await DisplayAlertAsync(
            "Delete Entry",
            $"Remove {item.TitleLine} from the local ledger?",
            "Delete",
            "Cancel");

        if (!shouldDelete)
        {
            return;
        }

        await ViewModel.DeleteTransactionAsync(item.Id);
    }

    private static HistoryTransactionItem? ResolveHistoryTransactionItem(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is HistoryTransactionItem itemFromEvent)
        {
            return itemFromEvent;
        }

        if (sender is BindableObject { BindingContext: HistoryTransactionItem itemFromSender })
        {
            return itemFromSender;
        }

        return null;
    }
}
