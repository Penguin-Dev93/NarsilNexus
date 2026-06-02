using System.IO;
using System.Text;
using NarsilNexus.App.ViewModels;

namespace NarsilNexus.App.Services;

public sealed class SimplePdfReportWriter
{
    public string WriteReport(string reportsDirectory, string target, IEnumerable<ToolRunViewModel> tools)
    {
        Directory.CreateDirectory(reportsDirectory);

        var fileName = $"NarsilNexus-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";
        var path = Path.Combine(reportsDirectory, fileName);

        var lines = new List<string>
        {
            "NarsilNexus Diagnostic Report",
            $"Generated: {DateTime.Now:G}",
            $"Target: {target}",
            string.Empty,
            "Results:"
        };

        var toolList = tools.ToList();
        var reportableTools = toolList
            .Where(tool => tool.Status is not NarsilNexus.Core.Diagnostics.DiagnosticStatus.Skipped
                and not NarsilNexus.Core.Diagnostics.DiagnosticStatus.Pending)
            .ToList();

        if (reportableTools.Count == 0)
        {
            lines.Add("No completed diagnostic results are available yet.");
        }
        else
        {
            foreach (var tool in reportableTools)
            {
                lines.Add($"{tool.Name}: {tool.Status} - {tool.Summary}");
            }
        }

        var skippedTools = toolList
            .Where(tool => tool.Status == NarsilNexus.Core.Diagnostics.DiagnosticStatus.Skipped)
            .Select(tool => tool.Name)
            .ToList();

        if (skippedTools.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"Unavailable or not configured: {string.Join(", ", skippedTools)}");
        }

        WriteSimplePdf(path, lines);
        return path;
    }

    private static void WriteSimplePdf(string path, IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 12 Tf");
        content.AppendLine("50 760 Td");

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                content.AppendLine("0 -18 Td");
            }

            content.Append('(');
            content.Append(EscapePdfText(lines[i]));
            content.AppendLine(") Tj");
        }

        content.AppendLine("ET");

        var stream = Encoding.ASCII.GetBytes(content.ToString());
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {stream.Length} >>\nstream\n{content}endstream"
        };

        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(file, Encoding.ASCII, leaveOpen: true);

        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };

        for (var i = 0; i < objects.Length; i++)
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
        writer.WriteLine($"0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");

        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
    }

    private static string EscapePdfText(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
