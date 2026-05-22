using FinanceTracker.Pages;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;
using Microsoft.Extensions.Logging;

namespace FinanceTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IFinanceDataService, FinanceDataService>();
        builder.Services.AddSingleton<OverviewViewModel>();
        builder.Services.AddSingleton<AddExpenseViewModel>();
        builder.Services.AddSingleton<BudgetViewModel>();
        builder.Services.AddSingleton<HistoryViewModel>();
        builder.Services.AddSingleton<OverviewPage>();
        builder.Services.AddSingleton<AddExpensePage>();
        builder.Services.AddSingleton<BudgetPage>();
        builder.Services.AddSingleton<HistoryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
