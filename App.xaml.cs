using FinanceTracker.Services;
using System.Diagnostics;

namespace FinanceTracker;

public partial class App : Application
{
    private readonly IFinanceDataService financeDataService;

    public App(IFinanceDataService financeDataService)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        this.financeDataService = financeDataService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Created += OnWindowCreated;
        return window;
    }

    private async void OnWindowCreated(object? sender, EventArgs e)
    {
        try
        {
            await financeDataService.InitializeAsync();

            if (!financeDataService.IsProfileComplete)
            {
                // Small delay to ensure Shell is fully loaded on Android
                await Task.Delay(300);

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (Shell.Current?.CurrentPage is Page currentPage)
                        {
                            await currentPage.DisplayAlertAsync(
                                "sys.profile_setup",
                                "Profile data not configured. Please enter your name, email, and add at least one bank account to initialize the local ledger.",
                                "[ configure ]");
                        }

                        await Shell.Current!.GoToAsync("//profile");
                    }
                    catch (Exception navEx)
                    {
                        Debug.WriteLine($"Failed to navigate to profile: {navEx}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize finance data: {ex}");
        }
    }
}
