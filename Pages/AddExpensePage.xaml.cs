using FinanceTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Pages;

public partial class AddExpensePage : ContentPage
{
    public AddExpensePage()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<AddExpenseViewModel>();
        FinanceTracker.Helpers.SwipeNavigationHelper.AddSwipeGestures(this, "overview", "budget");
    }
}
