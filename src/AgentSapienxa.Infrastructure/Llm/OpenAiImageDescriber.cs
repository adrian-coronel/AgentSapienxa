using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AgentSapienxa.Infrastructure.Llm;

public class OpenAiImageDescriber : IImageDescriber
{
    private readonly ChatClient _client;
    private readonly ILogger<OpenAiImageDescriber> _logger;

    public OpenAiImageDescriber(IConfiguration config, ILogger<OpenAiImageDescriber> logger)
    {
        var apiKey = config["OpenAI:ApiKey"] ?? string.Empty;
        _client = new ChatClient("gpt-4o", apiKey);
        _logger = logger;
    }

    public async Task<string> DescribeAsync(Stream imageStream, string mimeType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var messages = new List<ChatMessage>
        {
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    "Describe esta imagen en detalle para un agente de ventas. " +
                    "Si contiene un comprobante de pago, extrae: monto, banco y número de operación."),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), mimeType))
        };

        var response = await _client.CompleteChatAsync(messages, cancellationToken: ct);
        var description = response.Value.Content[0].Text;
        _logger.LogInformation("[Vision] Image described: {Chars} chars", description.Length);
        return description;
    }
}
