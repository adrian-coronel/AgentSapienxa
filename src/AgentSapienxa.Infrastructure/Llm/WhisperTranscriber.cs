using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace AgentSapienxa.Infrastructure.Llm;

public class WhisperTranscriber : IMediaTranscriber
{
    private readonly AudioClient _client;
    private readonly ILogger<WhisperTranscriber> _logger;

    public WhisperTranscriber(IConfiguration config, ILogger<WhisperTranscriber> logger)
    {
        var apiKey = config["OpenAI:ApiKey"] ?? string.Empty;
        _client = new AudioClient("whisper-1", apiKey);
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(Stream audioStream, string mimeType, CancellationToken ct = default)
    {
        var filename = $"audio{MimeTypeToExtension(mimeType)}";
        var result = await _client.TranscribeAudioAsync(audioStream, filename, cancellationToken: ct);
        _logger.LogInformation("[Whisper] Transcribed {Chars} chars", result.Value.Text.Length);
        return result.Value.Text;
    }

    private static string MimeTypeToExtension(string mimeType) =>
        mimeType.Split(';')[0].Trim() switch
        {
            "audio/ogg" => ".ogg",
            "audio/mpeg" => ".mp3",
            "audio/mp4" => ".mp4",
            "audio/wav" => ".wav",
            "audio/webm" => ".webm",
            "audio/aac" => ".aac",
            _ => ".ogg"
        };
}
