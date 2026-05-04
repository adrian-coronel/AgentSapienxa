using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Webhooks;
using AgentSapienxa.Infrastructure.Messaging.WhatsApp;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers.Webhooks;

[ApiController]
[Route("api/webhooks/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly MetaIncomingMessageMapper _mapper;
    private readonly IMediaTranscriber _transcriber;
    private readonly IImageDescriber _imageDescriber;
    private readonly IMediator _mediator;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IConfiguration config,
        MetaIncomingMessageMapper mapper,
        IMediaTranscriber transcriber,
        IImageDescriber imageDescriber,
        IMediator mediator,
        ILogger<WhatsAppWebhookController> logger)
    {
        _config = config;
        _mapper = mapper;
        _transcriber = transcriber;
        _imageDescriber = imageDescriber;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Verificación del webhook de Meta (GET).</summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = _config["Meta:VerifyToken"];
        if (mode == "subscribe" && token == expectedToken)
            return Ok(challenge);

        _logger.LogWarning("Webhook verification failed. Mode={Mode}", mode);
        return Forbid();
    }

    /// <summary>Recibe eventos de WhatsApp Business API (POST). Firma validada por middleware.</summary>
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] MetaWebhookPayload payload, CancellationToken ct)
    {
        var messages = await _mapper.MapAsync(payload, ct);

        foreach (var msg in messages)
        {
            // Resolver media a texto antes de pasarlo al orquestador
            if (msg.Type == IncomingMessageType.Audio && msg.MediaStream is not null)
                msg.Text = await _transcriber.TranscribeAsync(msg.MediaStream, msg.MimeType ?? "audio/ogg", ct);

            else if (msg.Type == IncomingMessageType.Image && msg.MediaStream is not null)
                msg.Text = await _imageDescriber.DescribeAsync(msg.MediaStream, msg.MimeType ?? "image/jpeg", ct);

            if (msg.Type == IncomingMessageType.Unknown)
            {
                _logger.LogInformation("[Webhook] Ignoring unsupported message type from {Session}", msg.SessionId);
                continue;
            }

            await _mediator.Send(new ProcessIncomingMessageCommand(msg), ct);
        }

        return Ok();
    }
}
