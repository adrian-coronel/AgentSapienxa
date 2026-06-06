using AgentSapienxa.Application.Common.Abstractions;
using Mammoth;
using ReverseMarkdown;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;

namespace AgentSapienxa.Infrastructure.Documents.Conversion;

public class PureDotNetMarkdownConverter : IDocumentToMarkdownConverter
{
    public async Task<string> ConvertAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        var normalized = contentType.Split(';')[0].Trim().ToLowerInvariant();

        return normalized switch
        {
            "application/pdf" => await ConvertPdfAsync(content, ct),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => await ConvertDocxAsync(content, ct),
            "text/markdown" => await ReadTextAsync(content, ct),
            "text/plain" => await ConvertTxtAsync(content, ct),
            _ when normalized.StartsWith("text/") => await ConvertTxtAsync(content, ct),
            _ => throw new NotSupportedException($"Tipo de documento no soportado: {contentType}")
        };
    }

    private static async Task<string> ConvertPdfAsync(Stream stream, CancellationToken ct)
    {
        await Task.CompletedTask; // PdfPig is synchronous

        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        double? lastFontSize = null;
        var fontSizes = new List<double>();

        // First pass: collect font sizes to detect headings
        foreach (var page in pdf.GetPages())
        {
            foreach (var word in page.GetWords())
                fontSizes.AddRange(word.Letters.Select(l => l.FontSize));
        }

        double medianSize = fontSizes.Count > 0 ? Median(fontSizes) : 12;
        double stdDev = fontSizes.Count > 0 ? StdDev(fontSizes, medianSize) : 2;
        double h1Threshold = medianSize + stdDev * 2.0;
        double h2Threshold = medianSize + stdDev * 1.2;
        double h3Threshold = medianSize + stdDev * 0.5;

        foreach (var page in pdf.GetPages())
        {
            string? currentLine = null;
            double currentFontSize = medianSize;

            foreach (var word in page.GetWords())
            {
                var wordFontSize = word.Letters.Select(l => l.FontSize).Average();

                if (lastFontSize.HasValue && Math.Abs(wordFontSize - lastFontSize.Value) > 1.0)
                {
                    if (currentLine is not null)
                    {
                        var prefix = GetHeadingPrefix(currentFontSize, h1Threshold, h2Threshold, h3Threshold);
                        sb.AppendLine(prefix + currentLine.Trim());
                        sb.AppendLine();
                        currentLine = null;
                    }
                }

                currentLine = (currentLine is null ? "" : currentLine + " ") + word.Text;
                currentFontSize = wordFontSize;
                lastFontSize = wordFontSize;
            }

            if (currentLine is not null)
            {
                var prefix = GetHeadingPrefix(currentFontSize, h1Threshold, h2Threshold, h3Threshold);
                sb.AppendLine(prefix + currentLine.Trim());
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static async Task<string> ConvertDocxAsync(Stream stream, CancellationToken ct)
    {
        await Task.CompletedTask; // Mammoth is synchronous
        var converter = new DocumentConverter();
        var result = converter.ConvertToHtml(stream);
        var converter2 = new Converter(new Config { UnknownTags = Config.UnknownTagsOption.Drop });
        return converter2.Convert(result.Value);
    }

    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(ct);
    }

    private static async Task<string> ConvertTxtAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct);
        // Normalize line endings and group as paragraphs (double newline = paragraph break)
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var paragraphs = new List<string>();
        var currentParagraph = new StringBuilder();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentParagraph.Length > 0)
                {
                    paragraphs.Add(currentParagraph.ToString().Trim());
                    currentParagraph.Clear();
                }
            }
            else
            {
                if (currentParagraph.Length > 0) currentParagraph.Append(' ');
                currentParagraph.Append(line.Trim());
            }
        }

        if (currentParagraph.Length > 0)
            paragraphs.Add(currentParagraph.ToString().Trim());

        return string.Join("\n\n", paragraphs);
    }

    private static string GetHeadingPrefix(double size, double h1, double h2, double h3) =>
        size >= h1 ? "# " :
        size >= h2 ? "## " :
        size >= h3 ? "### " :
        "";

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }

    private static double StdDev(List<double> values, double mean) =>
        Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Count);
}
