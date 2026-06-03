using System.Globalization;
using System.IO;
using System.Text;
using NarsilNexus.App.ViewModels;
using NarsilNexus.Core.Diagnostics;

namespace NarsilNexus.App.Services;

public sealed class SimplePdfReportWriter
{
    private const double PageWidth = 612;
    private const double PageHeight = 792;
    private const double Margin = 44;
    private const double ContentWidth = PageWidth - Margin * 2;

    public string WriteReport(string reportsDirectory, string target, IEnumerable<ToolRunViewModel> tools)
    {
        Directory.CreateDirectory(reportsDirectory);

        var generatedAt = DateTime.Now;
        var toolList = tools
            .Select(tool => new ReportToolResult(tool.Name, tool.Status, tool.Summary, tool.RawOutput ?? string.Empty))
            .ToList();

        return WriteReport(reportsDirectory, target, generatedAt, toolList);
    }

    public string WriteReport(string reportsDirectory, DiagnosticHistoryEntry entry)
    {
        Directory.CreateDirectory(reportsDirectory);

        var generatedAt = DateTime.Now;
        var fileName = $"NarsilNexus-{entry.StartedAt.LocalDateTime:yyyyMMdd-HHmmss}-history.pdf";
        var path = Path.Combine(reportsDirectory, fileName);
        var toolList = entry.Results
            .Select(result => new ReportToolResult(result.Name, result.Status, result.Summary, result.RawOutput))
            .ToList();

        return WriteReport(reportsDirectory, entry.Target, generatedAt, toolList, path);
    }

    private string WriteReport(
        string reportsDirectory,
        string target,
        DateTime generatedAt,
        IReadOnlyList<ReportToolResult> tools,
        string? explicitPath = null)
    {
        Directory.CreateDirectory(reportsDirectory);

        var path = explicitPath ?? Path.Combine(reportsDirectory, $"NarsilNexus-{generatedAt:yyyyMMdd-HHmmss}.pdf");
        var completedTools = tools
            .Where(tool => tool.Status is not DiagnosticStatus.Pending and not DiagnosticStatus.Running)
            .ToList();
        var reportableTools = completedTools
            .Where(tool => tool.Status != DiagnosticStatus.Skipped)
            .ToList();
        var skippedTools = completedTools
            .Where(tool => tool.Status == DiagnosticStatus.Skipped)
            .ToList();
        var rawOutputTools = reportableTools
            .Where(tool => string.IsNullOrWhiteSpace(tool.RawOutput) is false)
            .ToList();

        var pdf = new PdfReportBuilder();
        WriteHeader(pdf, target, generatedAt, reportableTools);
        WriteResults(pdf, reportableTools);
        WriteSkipped(pdf, skippedTools);
        WriteRawAppendix(pdf, rawOutputTools);
        pdf.Save(path);
        return path;
    }

    private static void WriteHeader(
        PdfReportBuilder pdf,
        string target,
        DateTime generatedAt,
        IReadOnlyList<ReportToolResult> reportableTools)
    {
        pdf.AddPage();
        pdf.WriteText("NarsilNexus Diagnostic Report", 22, PdfFont.Bold, PdfColor.Accent);
        pdf.WriteText($"Generated: {generatedAt:G}", 10, PdfFont.Regular, PdfColor.Secondary);
        pdf.WriteText($"Target: {target}", 12, PdfFont.Bold, PdfColor.Primary);
        pdf.AddGap(10);

        var pass = reportableTools.Count(tool => tool.Status == DiagnosticStatus.Pass);
        var warning = reportableTools.Count(tool => tool.Status == DiagnosticStatus.Warning);
        var fail = reportableTools.Count(tool => tool.Status is DiagnosticStatus.Fail or DiagnosticStatus.Blocked);
        var canceled = reportableTools.Count(tool => tool.Status == DiagnosticStatus.Canceled);

        pdf.DrawStatusCards([
            new StatusCard("Pass", pass, PdfColor.Pass),
            new StatusCard("Warning", warning, PdfColor.Warning),
            new StatusCard("Fail/Blocked", fail, PdfColor.Fail),
            new StatusCard("Canceled", canceled, PdfColor.Secondary)
        ]);

        pdf.AddGap(12);
        pdf.WriteSectionTitle("Executive Summary");
        if (reportableTools.Count == 0)
        {
            pdf.WriteMuted("No completed diagnostic results are available yet.");
            return;
        }

        var overall = fail > 0
            ? "Action required: one or more diagnostics failed."
            : warning > 0
                ? "Review recommended: one or more diagnostics returned warnings."
                : "No failures detected in the completed diagnostics.";
        pdf.WriteWrapped(overall, 11, PdfFont.Regular, PdfColor.Primary);
    }

    private static void WriteResults(PdfReportBuilder pdf, IReadOnlyList<ReportToolResult> reportableTools)
    {
        pdf.AddGap(14);
        pdf.WriteSectionTitle("Diagnostic Results");

        if (reportableTools.Count == 0)
        {
            pdf.WriteMuted("No completed diagnostic results are available yet.");
            return;
        }

        foreach (var tool in reportableTools)
        {
            pdf.WriteResultRow(tool.Name, tool.Status, tool.Summary);
        }
    }

    private static void WriteSkipped(PdfReportBuilder pdf, IReadOnlyList<ReportToolResult> skippedTools)
    {
        if (skippedTools.Count == 0)
        {
            return;
        }

        pdf.AddGap(12);
        pdf.WriteSectionTitle("Skipped Or Not Configured");
        foreach (var tool in skippedTools)
        {
            pdf.WriteWrapped($"{tool.Name}: {tool.Summary}", 10, PdfFont.Regular, PdfColor.Secondary);
        }
    }

    private static void WriteRawAppendix(PdfReportBuilder pdf, IReadOnlyList<ReportToolResult> rawOutputTools)
    {
        if (rawOutputTools.Count == 0)
        {
            return;
        }

        pdf.AddPage();
        pdf.WriteText("Raw Details Appendix", 18, PdfFont.Bold, PdfColor.Accent);
        pdf.WriteMuted("Raw command output and response details are preserved for audit and troubleshooting.");
        pdf.AddGap(8);

        foreach (var tool in rawOutputTools)
        {
            pdf.EnsureSpace(60);
            pdf.WriteSectionTitle(tool.Name);
            foreach (var line in NormalizeLines(tool.RawOutput!))
            {
                pdf.WriteWrapped(line, 8, PdfFont.Mono, PdfColor.Primary, 95);
            }

            pdf.AddGap(8);
        }
    }

    private static IEnumerable<string> NormalizeLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => string.IsNullOrWhiteSpace(line) ? " " : line.TrimEnd());
    }

    private sealed class PdfReportBuilder
    {
        private readonly List<List<string>> _pages = [];
        private List<string> _currentPage = [];
        private double _y;

        public void AddPage()
        {
            _currentPage = [];
            _pages.Add(_currentPage);
            _y = PageHeight - Margin;
            DrawFooter();
        }

        public void AddGap(double height)
        {
            _y -= height;
        }

        public void EnsureSpace(double height)
        {
            if (_pages.Count == 0 || _y - height < Margin)
            {
                AddPage();
            }
        }

        public void WriteSectionTitle(string text)
        {
            EnsureSpace(34);
            WriteText(text, 13, PdfFont.Bold, PdfColor.Primary);
            DrawLine(PdfColor.Border);
            AddGap(6);
        }

        public void WriteMuted(string text)
        {
            WriteWrapped(text, 10, PdfFont.Regular, PdfColor.Secondary);
        }

        public void WriteText(string text, double fontSize, PdfFont font, PdfColor color)
        {
            EnsureSpace(fontSize + 8);
            AddText(Margin, _y, text, fontSize, font, color);
            _y -= fontSize + 6;
        }

        public void WriteWrapped(
            string text,
            double fontSize,
            PdfFont font,
            PdfColor color,
            int maxChars = 98)
        {
            foreach (var line in WrapText(text, maxChars))
            {
                WriteText(line, fontSize, font, color);
            }
        }

        public void DrawStatusCards(IReadOnlyList<StatusCard> cards)
        {
            EnsureSpace(62);
            var cardWidth = (ContentWidth - 24) / 4;
            var x = Margin;
            foreach (var card in cards)
            {
                DrawFilledRectangle(x, _y - 48, cardWidth, 48, PdfColor.Surface);
                DrawRectangle(x, _y - 48, cardWidth, 48, PdfColor.Border);
                AddText(x + 10, _y - 18, card.Label, 9, PdfFont.Bold, PdfColor.Secondary);
                AddText(x + 10, _y - 36, card.Count.ToString(CultureInfo.InvariantCulture), 17, PdfFont.Bold, card.Color);
                x += cardWidth + 8;
            }

            _y -= 60;
        }

        public void WriteResultRow(string name, DiagnosticStatus status, string summary)
        {
            var summaryLines = WrapText(summary, 80).ToList();
            var rowHeight = Math.Max(48, 28 + summaryLines.Count * 13);
            EnsureSpace(rowHeight + 8);

            DrawFilledRectangle(Margin, _y - rowHeight, ContentWidth, rowHeight, PdfColor.Surface);
            DrawRectangle(Margin, _y - rowHeight, ContentWidth, rowHeight, PdfColor.Border);
            AddText(Margin + 12, _y - 18, status.ToString(), 10, PdfFont.Bold, StatusColor(status));
            AddText(Margin + 106, _y - 18, name, 11, PdfFont.Bold, PdfColor.Primary);

            var textY = _y - 34;
            foreach (var line in summaryLines)
            {
                AddText(Margin + 106, textY, line, 9, PdfFont.Regular, PdfColor.Secondary);
                textY -= 13;
            }

            _y -= rowHeight + 8;
        }

        public void Save(string path)
        {
            if (_pages.Count == 0)
            {
                AddPage();
            }

            var pageCount = _pages.Count;
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>"
            };

            var pageObjectIds = Enumerable.Range(0, pageCount)
                .Select(index => 3 + index * 2)
                .ToList();
            objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageCount} >>");

            for (var i = 0; i < pageCount; i++)
            {
                var contentObjectId = pageObjectIds[i] + 1;
                var stream = string.Join(Environment.NewLine, _pages[i]);
                var streamBytes = Encoding.ASCII.GetBytes(stream);
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 {pageCount * 2 + 3} 0 R /F2 {pageCount * 2 + 4} 0 R /F3 {pageCount * 2 + 5} 0 R >> >> /Contents {contentObjectId} 0 R >>");
                objects.Add($"<< /Length {streamBytes.Length} >>\nstream\n{stream}\nendstream");
            }

            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
            WritePdf(path, objects);
        }

        private void DrawFooter()
        {
            AddText(Margin, 24, "NarsilNexus", 8, PdfFont.Bold, PdfColor.Secondary);
            AddText(PageWidth - Margin - 118, 24, DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), 8, PdfFont.Regular, PdfColor.Secondary);
        }

        private void DrawLine(PdfColor color)
        {
            EnsureSpace(8);
            AddCommand($"{color.Stroke} {Margin:0.###} {_y:0.###} m {PageWidth - Margin:0.###} {_y:0.###} l S");
            _y -= 8;
        }

        private void DrawRectangle(double x, double y, double width, double height, PdfColor color)
        {
            AddCommand($"{color.Stroke} {x:0.###} {y:0.###} {width:0.###} {height:0.###} re S");
        }

        private void DrawFilledRectangle(double x, double y, double width, double height, PdfColor color)
        {
            AddCommand($"{color.Fill} {x:0.###} {y:0.###} {width:0.###} {height:0.###} re f");
        }

        private void AddText(double x, double y, string text, double fontSize, PdfFont font, PdfColor color)
        {
            var escaped = EscapePdfText(SanitizeText(text));
            AddCommand($"BT {color.Fill} /{font.Name} {fontSize:0.###} Tf {x:0.###} {y:0.###} Td ({escaped}) Tj ET");
        }

        private void AddCommand(string command)
        {
            _currentPage.Add(command);
        }

        private static PdfColor StatusColor(DiagnosticStatus status)
        {
            return status switch
            {
                DiagnosticStatus.Pass => PdfColor.Pass,
                DiagnosticStatus.Warning => PdfColor.Warning,
                DiagnosticStatus.Fail or DiagnosticStatus.Blocked => PdfColor.Fail,
                DiagnosticStatus.Canceled => PdfColor.Secondary,
                _ => PdfColor.Secondary
            };
        }
    }

    private sealed record StatusCard(string Label, int Count, PdfColor Color);

    private sealed record ReportToolResult(
        string Name,
        DiagnosticStatus Status,
        string Summary,
        string RawOutput);

    private sealed record PdfFont(string Name)
    {
        public static readonly PdfFont Regular = new("F1");
        public static readonly PdfFont Bold = new("F2");
        public static readonly PdfFont Mono = new("F3");
    }

    private sealed record PdfColor(double R, double G, double B)
    {
        public static readonly PdfColor Primary = FromHex(0x11, 0x18, 0x27);
        public static readonly PdfColor Secondary = FromHex(0x4B, 0x55, 0x63);
        public static readonly PdfColor Accent = FromHex(0x1D, 0x4E, 0x89);
        public static readonly PdfColor Surface = FromHex(0xF3, 0xF6, 0xFA);
        public static readonly PdfColor Border = FromHex(0xCB, 0xD5, 0xE1);
        public static readonly PdfColor Pass = FromHex(0x16, 0xA3, 0x4A);
        public static readonly PdfColor Warning = FromHex(0xB4, 0x53, 0x09);
        public static readonly PdfColor Fail = FromHex(0xDC, 0x26, 0x26);

        public string Fill => $"{R:0.###} {G:0.###} {B:0.###} rg";
        public string Stroke => $"{R:0.###} {G:0.###} {B:0.###} RG";

        private static PdfColor FromHex(int r, int g, int b)
        {
            return new PdfColor(r / 255d, g / 255d, b / 255d);
        }
    }

    private static IEnumerable<string> WrapText(string text, int maxChars)
    {
        var remaining = SanitizeText(text);
        if (string.IsNullOrWhiteSpace(remaining))
        {
            yield return " ";
            yield break;
        }

        while (remaining.Length > maxChars)
        {
            var split = remaining.LastIndexOf(' ', maxChars);
            if (split <= 0)
            {
                split = maxChars;
            }

            yield return remaining[..split].Trim();
            remaining = remaining[split..].TrimStart();
        }

        yield return remaining;
    }

    private static void WritePdf(string path, IReadOnlyList<string> objects)
    {
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(file, Encoding.ASCII, leaveOpen: true);

        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };

        for (var i = 0; i < objects.Count; i++)
        {
            writer.Flush();
            offsets.Add(file.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = file.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");

        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
    }

    private static string SanitizeText(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character is >= ' ' and <= '~' ? character : '?');
        }

        return builder.ToString();
    }

    private static string EscapePdfText(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
