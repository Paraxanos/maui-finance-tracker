using System.Globalization;
using System.Text;

namespace FinanceTracker.Helpers;

internal sealed class SimplePdfDocumentBuilder
{
    public const double A4Width = 595.0;
    public const double A4Height = 842.0;

    private readonly List<PdfPageBuilder> _pages = [];

    public PdfPageBuilder AddPage()
    {
        var page = new PdfPageBuilder();
        _pages.Add(page);
        return page;
    }

    public byte[] Build()
    {
        if (_pages.Count == 0)
            throw new InvalidOperationException("At least one page is required.");

        const int catalogId = 1;
        const int pagesId = 2;
        const int regularFontId = 3;
        const int boldFontId = 4;

        var nextId = 5;
        var pageObjectIds = new List<int>(_pages.Count);
        var contentObjectIds = new List<int>(_pages.Count);
        foreach (var _ in _pages)
        {
            pageObjectIds.Add(nextId++);
            contentObjectIds.Add(nextId++);
        }

        var objects = new byte[nextId][];
        objects[regularFontId] = Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
        objects[boldFontId] = Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold >>");

        for (var i = 0; i < _pages.Count; i++)
        {
            var pageId = pageObjectIds[i];
            var contentId = contentObjectIds[i];
            var pageContent = Ascii(_pages[i].BuildContent());

            objects[contentId] = BuildStreamObject(pageContent);
            objects[pageId] = Ascii(
                $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {Fmt(A4Width)} {Fmt(A4Height)}] " +
                $"/Resources << /Font << /F1 {regularFontId} 0 R /F2 {boldFontId} 0 R >> >> " +
                $"/Contents {contentId} 0 R >>");
        }

        var kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        objects[pagesId] = Ascii($"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>");
        objects[catalogId] = Ascii($"<< /Type /Catalog /Pages {pagesId} 0 R >>");

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Ascii("%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n"));

        var offsets = new long[nextId];
        for (var id = 1; id < nextId; id++)
        {
            offsets[id] = stream.Position;
            writer.Write(Ascii($"{id} 0 obj\n"));
            writer.Write(objects[id]);
            writer.Write(Ascii("\nendobj\n"));
        }

        var xrefOffset = stream.Position;
        writer.Write(Ascii($"xref\n0 {nextId}\n"));
        writer.Write(Ascii("0000000000 65535 f \n"));
        for (var id = 1; id < nextId; id++)
            writer.Write(Ascii($"{offsets[id]:0000000000} 00000 n \n"));

        writer.Write(Ascii(
            $"trailer\n<< /Size {nextId} /Root {catalogId} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] BuildStreamObject(byte[] content)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Ascii($"<< /Length {content.Length} >>\nstream\n"));
        writer.Write(content);
        writer.Write(Ascii("\nendstream"));
        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

    private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    internal enum PdfFont
    {
        Regular,
        Bold
    }

    internal readonly record struct PdfColor(byte R, byte G, byte B)
    {
        public static PdfColor FromRgb(byte r, byte g, byte b) => new(r, g, b);

        public string ToFillCommand() => $"{ToUnit(R)} {ToUnit(G)} {ToUnit(B)} rg";

        public string ToStrokeCommand() => $"{ToUnit(R)} {ToUnit(G)} {ToUnit(B)} RG";

        private static string ToUnit(byte value) =>
            (value / 255d).ToString("0.###", CultureInfo.InvariantCulture);
    }

    internal sealed class PdfPageBuilder
    {
        private readonly StringBuilder _content = new();

        internal string BuildContent() => _content.ToString();

        public void DrawText(double x, double yFromTop, string text, double fontSize, PdfFont font, PdfColor color)
        {
            var safeText = EscapeText(text);
            var pdfY = A4Height - yFromTop;
            var fontName = font == PdfFont.Bold ? "F2" : "F1";

            _content.AppendLine("BT");
            _content.AppendLine($"{color.ToFillCommand()}");
            _content.AppendLine($"/{fontName} {Fmt(fontSize)} Tf");
            _content.AppendLine($"1 0 0 1 {Fmt(x)} {Fmt(pdfY)} Tm");
            _content.AppendLine($"({safeText}) Tj");
            _content.AppendLine("ET");
        }

        public void DrawLine(double x1, double y1FromTop, double x2, double y2FromTop, PdfColor color, double width)
        {
            var pdfY1 = A4Height - y1FromTop;
            var pdfY2 = A4Height - y2FromTop;

            _content.AppendLine($"{color.ToStrokeCommand()}");
            _content.AppendLine($"{Fmt(width)} w");
            _content.AppendLine($"{Fmt(x1)} {Fmt(pdfY1)} m");
            _content.AppendLine($"{Fmt(x2)} {Fmt(pdfY2)} l");
            _content.AppendLine("S");
        }

        private static string EscapeText(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                switch (ch)
                {
                    case '\\':
                    case '(':
                    case ')':
                        builder.Append('\\').Append(ch);
                        break;
                    case '\r':
                    case '\n':
                    case '\t':
                        builder.Append(' ');
                        break;
                    case >= ' ' and <= '~':
                        builder.Append(ch);
                        break;
                    case '\u20B9':
                        builder.Append("Rs");
                        break;
                    default:
                        builder.Append('?');
                        break;
                }
            }

            return builder.ToString();
        }

        private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
