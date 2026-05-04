using System.Text.Json.Serialization;

namespace AgentSapienxa.Infrastructure.Messaging.WhatsApp;

public class MetaWebhookPayload
{
    [JsonPropertyName("entry")] public List<MetaEntry> Entry { get; set; } = [];
}

public class MetaEntry
{
    [JsonPropertyName("changes")] public List<MetaChange> Changes { get; set; } = [];
}

public class MetaChange
{
    [JsonPropertyName("field")] public string Field { get; set; } = string.Empty;
    [JsonPropertyName("value")] public MetaChangeValue Value { get; set; } = new();
}

public class MetaChangeValue
{
    [JsonPropertyName("messaging_product")] public string MessagingProduct { get; set; } = string.Empty;
    [JsonPropertyName("metadata")] public MetaMetadata Metadata { get; set; } = new();
    [JsonPropertyName("contacts")] public List<MetaContact> Contacts { get; set; } = [];
    [JsonPropertyName("messages")] public List<MetaMessage> Messages { get; set; } = [];
    [JsonPropertyName("statuses")] public List<MetaStatus> Statuses { get; set; } = [];
}

public class MetaMetadata
{
    [JsonPropertyName("display_phone_number")] public string DisplayPhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("phone_number_id")] public string PhoneNumberId { get; set; } = string.Empty;
}

public class MetaContact
{
    [JsonPropertyName("profile")] public MetaProfile Profile { get; set; } = new();
    [JsonPropertyName("wa_id")] public string WaId { get; set; } = string.Empty;
}

public class MetaProfile
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

public class MetaMessage
{
    [JsonPropertyName("from")] public string From { get; set; } = string.Empty;
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("text")] public MetaTextMessage? Text { get; set; }
    [JsonPropertyName("audio")] public MetaMediaMessage? Audio { get; set; }
    [JsonPropertyName("image")] public MetaMediaMessage? Image { get; set; }
    [JsonPropertyName("document")] public MetaMediaMessage? Document { get; set; }
}

public class MetaTextMessage
{
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
}

public class MetaMediaMessage
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("mime_type")] public string MimeType { get; set; } = string.Empty;
    [JsonPropertyName("caption")] public string? Caption { get; set; }
    [JsonPropertyName("filename")] public string? Filename { get; set; }
}

public class MetaStatus
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("recipient_id")] public string RecipientId { get; set; } = string.Empty;
}
