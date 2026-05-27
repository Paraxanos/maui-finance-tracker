using FinanceTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Pages;

public partial class BudgetPage : ContentPage
{
    private BudgetViewModel? ViewModel =>
        BindingContext as BudgetViewModel;

    public BudgetPage()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<BudgetViewModel>();
        FinanceTracker.Helpers.SwipeNavigationHelper.AddSwipeGestures(this, "add-expense", "history");
    }

    private void OnPreviousMonthTapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.PreviousMonth();
    }

    private void OnNextMonthTapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.NextMonth();
    }

    private async void OnJumpMonthTapped(object? sender, TappedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        var monthText = await DisplayPromptAsync(
            "Jump To Month",
            "Enter a month as yyyy-MM",
            initialValue: viewModel.SelectedMonth.ToString("yyyy-MM"));

        if (monthText is null)
        {
            return;
        }

        if (!DateTime.TryParseExact(
                monthText.Trim(),
                "yyyy-MM",
                null,
                System.Globalization.DateTimeStyles.None,
                out var targetMonth))
        {
            await DisplayAlertAsync("Jump To Month", "Use a valid month such as 2026-05.", "OK");
            return;
        }

        viewModel.SelectMonth(targetMonth);
    }

    private async void OnSetBudgetTapped(object? sender, TappedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        if (ResolveBudgetItem(sender, e) is not { } item)
        {
            return;
        }

        var amountText = await DisplayPromptAsync(
            "Set Budget",
            $"Limit for {item.CategoryKey} in {viewModel.SelectedMonth:MMMM yyyy}",
            keyboard: Keyboard.Numeric,
            initialValue: item.HasBudget ? item.Limit.ToString("0.##") : string.Empty);

        if (amountText is null)
        {
            return;
        }

        if (!decimal.TryParse(amountText, out var amount) || amount <= 0m)
        {
            await DisplayAlertAsync("Set Budget", "Budget limit must be a positive number.", "OK");
            return;
        }

        await viewModel.SetBudgetAsync(item.Category, amount);
    }

    private async void OnClearBudgetTapped(object? sender, TappedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        if (ResolveBudgetItem(sender, e) is not { } item)
        {
            return;
        }

        var shouldClear = await DisplayAlertAsync(
            "Clear Budget",
            $"Remove the limit for {item.CategoryKey} in {viewModel.SelectedMonth:MMMM yyyy}?",
            "Clear",
            "Cancel");

        if (!shouldClear)
        {
            return;
        }

        await viewModel.ClearBudgetAsync(item.Category);
    }

    private static BudgetCategoryItem? ResolveBudgetItem(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is BudgetCategoryItem itemFromEvent)
        {
            return itemFromEvent;
        }

        if (sender is BindableObject { BindingContext: BudgetCategoryItem itemFromSender })
        {
            return itemFromSender;
        }

        return null;
    }
}
