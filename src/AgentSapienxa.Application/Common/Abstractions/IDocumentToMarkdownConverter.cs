namespace AgentSapienxa.Application.Common.Abstractions;

public interface IDocumentToMarkdownConverter
{
    Task<string> ConvertAsync(Stream content, string contentType, CancellationToken ct = default);
}
