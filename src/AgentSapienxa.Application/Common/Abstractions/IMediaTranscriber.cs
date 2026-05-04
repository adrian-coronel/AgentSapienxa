namespace AgentSapienxa.Application.Common.Abstractions;

public interface IMediaTranscriber
{
    Task<string> TranscribeAsync(Stream audioStream, string mimeType, CancellationToken ct = default);
}
