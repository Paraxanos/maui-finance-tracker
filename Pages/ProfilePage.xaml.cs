using FinanceTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<ProfileViewModel>();

        // Scoped Export ViewModel — separate BindingContext for the export section
        ExportPanel.BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<ExportViewModel>();
        ExportControls.BindingContext = ExportPanel.BindingContext;

        FinanceTracker.Helpers.SwipeNavigationHelper.AddSwipeGestures(this, "history", "overview");
    }
}
