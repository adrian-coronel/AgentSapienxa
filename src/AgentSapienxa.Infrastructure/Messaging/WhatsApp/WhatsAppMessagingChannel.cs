using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Infrastructure.Messaging.WhatsApp;

public class WhatsAppMessagingChannel : IMessagingChannel
{
    private readonly MetaWhatsAppClient _client;
    private readonly ILogger<WhatsAppMessagingChannel> _logger;

    public WhatsAppMessagingChannel(MetaWhatsAppClient client, ILogger<WhatsAppMessagingChannel> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendTextAsync(string recipient, string message, CancellationToken ct = default)
    {
        // Meta Graph API expects phone number without leading '+' (wa_id format)
        var waId = recipient.TrimStart('+');
        _logger.LogInformation("[WhatsApp] → {Recipient}: {Preview}",
            recipient, message.Length > 80 ? message[..80] + "…" : message);
        await _client.SendTextAsync(waId, message, ct);
    }
}
