using PdfSharpCore.Drawing;
using Microsoft.Maui.Storage;

namespace FinanceTracker.Helpers;

public static class PdfFontResolver
{
    private static string? _cachedFontPath;
    private static readonly object _lock = new();

    public static async Task<string> GetMonoFontPathAsync(CancellationToken ct = default)
    {
        if (_cachedFontPath != null && File.Exists(_cachedFontPath))
            return _cachedFontPath;

        lock (_lock)
        {
            if (_cachedFontPath != null && File.Exists(_cachedFontPath))
                return _cachedFontPath;

            var destPath = Path.Combine(FileSystem.CacheDirectory, "JetBrainsMono-Regular.ttf");
            if (!File.Exists(destPath))
            {
                using var src = FileSystem.OpenAppPackageFileAsync("Fonts/JetBrainsMono-Regular.ttf").GetAwaiter().GetResult();
                using var dest = File.Create(destPath);
                src.CopyTo(dest);
            }
            _cachedFontPath = destPath;
        }

        XPrivateFontCollection.Instance.AddFromFile(_cachedFontPath);
        return _cachedFontPath;
    }

    public static XFont CreateMonoFont(double size, XFontStyleEx style = XFontStyleEx.Regular)
    {
        var fontPath = _cachedFontPath ?? throw new InvalidOperationException("Font not resolved. Call GetMonoFontPathAsync first.");
        return new XFont("JetBrains Mono", size, style, new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.Always));
    }
}
