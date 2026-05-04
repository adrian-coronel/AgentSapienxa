namespace AgentSapienxa.Application.Webhooks;

public enum IncomingMessageType { Text, Audio, Image, Document, Unknown }

public class IncomingMessage
{
    public string SessionId { get; init; } = default!;
    public string ContactName { get; init; } = string.Empty;
    public IncomingMessageType Type { get; init; }
    public string? Text { get; set; }
    public Stream? MediaStream { get; init; }
    public string? MimeType { get; init; }
    public string? Caption { get; init; }
    public string RawMessageId { get; init; } = string.Empty;
}
