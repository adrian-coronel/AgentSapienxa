using AgentSapienxa.Application.Webhooks;

namespace AgentSapienxa.Infrastructure.Messaging.WhatsApp;

public class MetaIncomingMessageMapper
{
    private readonly MetaWhatsAppClient _client;

    public MetaIncomingMessageMapper(MetaWhatsAppClient client) => _client = client;

    public async Task<IReadOnlyList<IncomingMessage>> MapAsync(MetaWebhookPayload payload, CancellationToken ct = default)
    {
        var result = new List<IncomingMessage>();

        foreach (var entry in payload.Entry)
        foreach (var change in entry.Changes)
        {
            if (change.Field != "messages") continue;

            var value = change.Value;
            var contactName = value.Contacts
                .ToDictionary(c => c.WaId, c => c.Profile.Name);

            foreach (var msg in value.Messages)
            {
                var name = contactName.TryGetValue(msg.From, out var n) ? n : string.Empty;
                var incoming = await MapMessageAsync(msg, name, ct);
                if (incoming is not null) result.Add(incoming);
            }
        }

        return result;
    }

    private async Task<IncomingMessage?> MapMessageAsync(MetaMessage msg, string contactName, CancellationToken ct)
    {
        return msg.Type switch
        {
            "text" => new IncomingMessage
            {
                SessionId = msg.From,
                ContactName = contactName,
                Type = IncomingMessageType.Text,
                Text = msg.Text?.Body,
                RawMessageId = msg.Id
            },
            "audio" when msg.Audio is not null =>
                await MapMediaAsync(msg, msg.Audio, IncomingMessageType.Audio, contactName, ct),
            "image" when msg.Image is not null =>
                await MapMediaAsync(msg, msg.Image, IncomingMessageType.Image, contactName, ct),
            "document" when msg.Document is not null =>
                await MapMediaAsync(msg, msg.Document, IncomingMessageType.Document, contactName, ct),
            _ => new IncomingMessage
            {
                SessionId = msg.From,
                ContactName = contactName,
                Type = IncomingMessageType.Unknown,
                RawMessageId = msg.Id
            }
        };
    }

    private async Task<IncomingMessage> MapMediaAsync(
        MetaMessage msg,
        MetaMediaMessage media,
        IncomingMessageType type,
        string contactName,
        CancellationToken ct)
    {
        var (stream, mimeType) = await _client.DownloadMediaAsync(media.Id, media.MimeType, ct);
        return new IncomingMessage
        {
            SessionId = msg.From,
            ContactName = contactName,
            Type = type,
            MediaStream = stream,
            MimeType = mimeType,
            Caption = media.Caption,
            RawMessageId = msg.Id
        };
    }
}
