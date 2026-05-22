using FinanceTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Pages;

public partial class OverviewPage : ContentPage
{
    private OverviewViewModel ViewModel =>
        (OverviewViewModel)(BindingContext ?? throw new InvalidOperationException("Missing view model."));

    public OverviewPage()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<OverviewViewModel>();
    }

    private void OnPreviousMonthTapped(object? sender, TappedEventArgs e)
    {
        ViewModel.PreviousMonth();
    }

    private void OnNextMonthTapped(object? sender, TappedEventArgs e)
    {
        ViewModel.NextMonth();
    }

    private void OnCalendarDayTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not MonthCalendarDayItem item || !item.IsSelectable)
        {
            return;
        }

        ViewModel.SelectDate(item.Date);
    }
}
