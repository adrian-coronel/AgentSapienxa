using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace AgentSapienxa.Infrastructure.Llm;

public class OpenAiImageDescriber : IImageDescriber
{
    private readonly OpenAIClient _client;
    private readonly string _model;
    private readonly ILogger<OpenAiImageDescriber> _logger;

    public OpenAiImageDescriber(OpenAIClient client, IConfiguration config, ILogger<OpenAiImageDescriber> logger)
    {
        _client = client;
        _model = config["OpenAI:VisionModel"] ?? "llama-3.2-11b-vision-preview";
        _logger = logger;
    }

    public async Task<string> DescribeAsync(Stream imageStream, string mimeType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var chatClient = _client.GetChatClient(_model);

        var messages = new List<ChatMessage>
        {
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    "Describe esta imagen en detalle para un agente de ventas. " +
                    "Si contiene un comprobante de pago, extrae: monto, banco y número de operación."),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), mimeType))
        };

        var response = await chatClient.CompleteChatAsync(messages, cancellationToken: ct);
        var description = response.Value.Content[0].Text;
        _logger.LogInformation("[Vision] Image described: {Chars} chars", description.Length);
        return description;
    }
}
