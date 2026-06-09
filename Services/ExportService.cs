using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceTracker.Helpers;
using FinanceTracker.Models;
using Microsoft.Maui.Storage;

namespace FinanceTracker.Services;

public sealed class ExportService : IExportService
{
    private readonly IFinanceDataService _data;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExportService(IFinanceDataService data)
    {
        _data = data;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<byte[]> ExportToJsonAsync(ExportOptions options, CancellationToken ct = default)
    {
        var snapshot = BuildSnapshot(options);
        using var ms = new MemoryStream();
        await JsonSerializer.SerializeAsync(ms, snapshot, _jsonOptions, ct);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportToPdfAsync(ExportOptions options, CancellationToken ct = default)
    {
        const double bodyFontSize = 9;
        const double headerFontSize = 10;
        var lineHeight = bodyFontSize * 1.45;

        var document = new SimplePdfDocumentBuilder();
        var page = document.AddPage();
        double x = 40, y = 40;
        double pageHeight = SimplePdfDocumentBuilder.A4Height;
        double margin = 40;

        var snapshot = BuildSnapshot(options);

        // Terminal Colors (mapped from Colors.xaml)
        var bodyText = SimplePdfDocumentBuilder.PdfColor.FromRgb(0x24, 0x2B, 0x33);
        var muted = SimplePdfDocumentBuilder.PdfColor.FromRgb(0x6C, 0x7A, 0x89);
        var accent = SimplePdfDocumentBuilder.PdfColor.FromRgb(0xF5, 0xA6, 0x23);
        var negative = SimplePdfDocumentBuilder.PdfColor.FromRgb(0xE0, 0x6C, 0x75);
        var positive = SimplePdfDocumentBuilder.PdfColor.FromRgb(0x56, 0xB6, 0xC2);

        page.DrawText(x, y, "FINANCE.TRACKER // Export", headerFontSize, SimplePdfDocumentBuilder.PdfFont.Bold, accent);
        y += lineHeight;
        page.DrawText(
            x,
            y,
            $"// {snapshot.ExportedAtUtc:yyyy-MM-dd HH:mm UTC} | v{snapshot.AppVersion}",
            bodyFontSize,
            SimplePdfDocumentBuilder.PdfFont.Regular,
            muted);
        y += lineHeight * 2;

        double colDate = x, colTitle = x + 85, colType = x + 240, colAmount = x + 310;

        page.DrawText(colDate, y, "DATE", bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, muted);
        page.DrawText(colTitle, y, "TITLE", bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, muted);
        page.DrawText(colType, y, "TYPE", bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, muted);
        page.DrawText(colAmount, y, "AMOUNT", bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, muted);
        y += lineHeight;
        page.DrawLine(x, y, SimplePdfDocumentBuilder.A4Width - margin, y, muted, 0.5);
        y += lineHeight;

        var sortedTxns = snapshot.Transactions.OrderBy(t => t.EntryDate).ToList();

        foreach (var t in sortedTxns)
        {
            if (y > pageHeight - margin - lineHeight * 2)
            {
                page = document.AddPage();
                y = margin;
            }

            var isIncome = t.EntryType == FinanceEntryType.Income;
            var amount = isIncome ? t.Amount : -t.Amount;
            var amountColor = amount >= 0 ? positive : negative;

            page.DrawText(colDate, y, t.EntryDate.ToString("yyyy-MM-dd"), bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, bodyText);
            page.DrawText(colTitle, y, TrimToColumn(t.Title, 24), bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, bodyText);
            page.DrawText(colType, y, t.EntryType.ToString().ToUpperInvariant(), bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, muted);
            page.DrawText(colAmount, y, FormatAmount(amount), bodyFontSize, SimplePdfDocumentBuilder.PdfFont.Regular, amountColor);

            y += lineHeight;
        }

        if (y > pageHeight - margin)
        {
            page = document.AddPage();
            y = margin;
        }
        page.DrawText(
            x,
            y + lineHeight * 2,
            $"// EOF | {sortedTxns.Count} records",
            bodyFontSize,
            SimplePdfDocumentBuilder.PdfFont.Regular,
            muted);

        return document.Build();
    }

    public async Task ShareFileAsync(byte[] data, string fileName, string mimeType, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllBytesAsync(tempPath, data, ct);

        await Share.RequestAsync(new ShareFileRequest
        {
            Title = "Export Finance Report",
            File = new ShareFile(tempPath, mimeType)
        });
    }

    private ExportSnapshot BuildSnapshot(ExportOptions options)
    {
        var txns = _data.Transactions
            .Where(t => options.AccountId == null || t.BankAccountId == options.AccountId)
            .Where(t => t.EntryDate >= options.StartDate.Date && t.EntryDate <= options.EndDate.Date)
            .OrderBy(t => t.EntryDate)
            .ToList();

        var budgets = options.IncludeBudgets
            ? _data.Budgets.Where(b => b.BudgetMonth >= options.StartDate.Date && b.BudgetMonth <= options.EndDate.Date).ToList()
            : [];

        return new ExportSnapshot(
            AppVersion: AppInfo.VersionString,
            ExportedAtUtc: DateTime.UtcNow,
            Profile: options.IncludeProfile ? _data.Profile : null,
            Accounts: options.IncludeProfile ? _data.Profile.BankAccounts : [],
            Transactions: txns,
            Budgets: budgets,
            Summary: new ExportSummary(
                TotalIncome: txns.Where(t => t.EntryType == FinanceEntryType.Income).Sum(t => t.Amount),
                TotalExpense: txns.Where(t => t.EntryType == FinanceEntryType.Expense).Sum(t => t.Amount),
                Net: FinanceMath.Net(txns),
                RecordCount: txns.Count)
        );
    }

    private static string FormatAmount(decimal amount) =>
        amount.ToString("+#,##0.00;-#,##0.00", CultureInfo.InvariantCulture);

    private static string TrimToColumn(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..(maxLength - 3)]}...";
}
