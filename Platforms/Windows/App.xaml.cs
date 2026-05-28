
using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace FinanceTracker.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            File.WriteAllText(@"D:\finance-tracker\crash2.txt", e.ExceptionObject.ToString());
        };
        this.UnhandledException += (s, e) => {
            File.WriteAllText(@"D:\finance-tracker\crash3.txt", e.Exception.ToString());
        };
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

