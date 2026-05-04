using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace AgentSapienxa.Infrastructure.Llm;

public class WhisperTranscriber : IMediaTranscriber
{
    private readonly OpenAIClient _client;
    private readonly string _model;
    private readonly ILogger<WhisperTranscriber> _logger;

    public WhisperTranscriber(OpenAIClient client, IConfiguration config, ILogger<WhisperTranscriber> logger)
    {
        _client = client;
        _model = config["OpenAI:TranscriptionModel"] ?? "whisper-large-v3-turbo";
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(Stream audioStream, string mimeType, CancellationToken ct = default)
    {
        var audioClient = _client.GetAudioClient(_model);
        var filename = $"audio{MimeTypeToExtension(mimeType)}";
        var result = await audioClient.TranscribeAudioAsync(audioStream, filename, cancellationToken: ct);
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
