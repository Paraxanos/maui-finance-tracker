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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize finance data: {ex}");
        }
    }
}
