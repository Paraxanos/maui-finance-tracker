using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceTracker.Helpers;
using FinanceTracker.Models;
using Microsoft.Maui.Storage;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

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
        await PdfFontResolver.GetMonoFontPathAsync(ct);
        var font = PdfFontResolver.CreateMonoFont(9);
        var headerFont = PdfFontResolver.CreateMonoFont(10, XFontStyleEx.Bold);
        var lineHeight = font.Height * 1.25;

        using var document = new PdfDocument();
        document.Info.Title = "Finance Tracker Export";
        document.Info.Creator = $"FinanceTracker v{AppInfo.VersionString}";

        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);
        double x = 40, y = 40;
        double pageHeight = page.Height;
        double margin = 40;

        var snapshot = BuildSnapshot(options);

        // Terminal Colors (mapped from Colors.xaml)
        var textWhite = XColor.FromArgb(0xE6, 0xE6, 0xE6);
        var muted = XColor.FromArgb(0x6C, 0x7A, 0x89);
        var accent = XColor.FromArgb(0xF5, 0xA6, 0x23);
        var negative = XColor.FromArgb(0xE0, 0x6C, 0x75);
        var positive = XColor.FromArgb(0x56, 0xB6, 0xC2);

        gfx.DrawString("FINANCE.TRACKER // Export", headerFont, new XSolidBrush(accent), x, y);
        y += lineHeight;
        gfx.DrawString($"// {snapshot.ExportedAtUtc:yyyy-MM-dd HH:mm UTC} | v{snapshot.AppVersion}",
                       font, new XSolidBrush(muted), x, y);
        y += lineHeight * 2;

        double colDate = x, colTitle = x + 85, colType = x + 240, colAmount = x + 310;

        gfx.DrawString("DATE", font, new XSolidBrush(muted), colDate, y);
        gfx.DrawString("TITLE", font, new XSolidBrush(muted), colTitle, y);
        gfx.DrawString("TYPE", font, new XSolidBrush(muted), colType, y);
        gfx.DrawString("AMOUNT", font, new XSolidBrush(muted), colAmount, y);
        y += lineHeight;
        gfx.DrawLine(new XPen(muted, 0.5), x, y, page.Width - margin, y);
        y += lineHeight;

        var sortedTxns = snapshot.Transactions.OrderBy(t => t.EntryDate).ToList();

        foreach (var t in sortedTxns)
        {
            if (y > pageHeight - margin - lineHeight * 2)
            {
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = margin;
            }

            var isIncome = t.EntryType == FinanceEntryType.Income;
            var amount = isIncome ? t.Amount : -t.Amount;
            var amountColor = amount >= 0 ? positive : negative;

            gfx.DrawString(t.EntryDate.ToString("yyyy-MM-dd"), font, new XSolidBrush(textWhite), colDate, y);
            gfx.DrawString(t.Title, font, new XSolidBrush(textWhite), colTitle, y);
            gfx.DrawString(t.EntryType.ToString().ToUpperInvariant(), font, new XSolidBrush(muted), colType, y);
            gfx.DrawString($"{amount:C2}", font, new XSolidBrush(amountColor), colAmount, y);

            y += lineHeight;
        }

        if (y > pageHeight - margin)
        {
            page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            y = margin;
        }
        gfx.DrawString($"// EOF | {sortedTxns.Count} records", font, new XSolidBrush(muted), x, y + lineHeight * 2);

        using var pdfMs = new MemoryStream();
        document.Save(pdfMs, false);
        return pdfMs.ToArray();
    }

    public async Task ShareFileAsync(byte[] data, string fileName, string mimeType, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllBytesAsync(tempPath, data, ct);

        await Share.RequestAsync(new ShareFileRequest
        {
            Title = "Export Finance Report",
            File = new ShareFile(tempPath),
            MimeType = mimeType
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
}
