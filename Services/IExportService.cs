using FinanceTracker.Models;

namespace FinanceTracker.Services;

public interface IExportService
{
    Task<byte[]> ExportToJsonAsync(ExportOptions options, CancellationToken ct = default);
    Task<byte[]> ExportToPdfAsync(ExportOptions options, CancellationToken ct = default);
    Task ShareFileAsync(byte[] data, string fileName, string mimeType, CancellationToken ct = default);
}
