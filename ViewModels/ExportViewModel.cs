using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Models;
using FinanceTracker.Services;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.ViewModels;

public sealed partial class ExportViewModel : ObservableObject
{
    private readonly IExportService _exportService;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddMonths(-6);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private string _statusMessage = "ready to export";
    [ObservableProperty] private Color _statusMessageColor = Color.FromArgb("#56B6C2");

    public ExportViewModel(IExportService exportService) => _exportService = exportService;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExportJsonAsync() => await ExecuteExportAsync("finance_export.json", "application/json", opts => _exportService.ExportToJsonAsync(opts));

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExportPdfAsync() => await ExecuteExportAsync("finance_report.pdf", "application/pdf", opts => _exportService.ExportToPdfAsync(opts));

    private async Task ExecuteExportAsync(string fileName, string mimeType, Func<ExportOptions, Task<byte[]>> generator)
    {
        if (IsBusy) return;
        if (StartDate.Date > EndDate.Date)
        {
            SetStatus("[ERR] start date cannot be after end date", "#E06C75");
            return;
        }

        IsBusy = true;
        SetStatus("compiling ledger data...", "#F5A623");

        try
        {
            var options = new ExportOptions(StartDate.Date, EndDate.Date, null, true, true);
            var data = await generator(options);
            await _exportService.ShareFileAsync(data, fileName, mimeType);
            SetStatus("[OK] export shared successfully", "#56B6C2");
        }
        catch (Exception ex)
        {
            SetStatus($"[ERR] {ex.Message}", "#E06C75");
        }
        finally { IsBusy = false; }
    }

    private void SetStatus(string message, string hexColor)
    {
        StatusMessage = message;
        StatusMessageColor = Color.FromArgb(hexColor);
    }
}
