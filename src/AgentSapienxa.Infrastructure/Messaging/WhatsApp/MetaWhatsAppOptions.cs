namespace AgentSapienxa.Infrastructure.Messaging.WhatsApp;

public class MetaWhatsAppOptions
{
    public const string Section = "Meta";
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v20.0";
}
