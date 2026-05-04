using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Infrastructure.Messaging.WhatsApp;

public class MetaWhatsAppClient
{
    private readonly HttpClient _http;
    private readonly MetaWhatsAppOptions _opts;
    private readonly ILogger<MetaWhatsAppClient> _logger;

    public MetaWhatsAppClient(HttpClient http, IOptions<MetaWhatsAppOptions> opts, ILogger<MetaWhatsAppClient> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task SendTextAsync(string toPhoneNumber, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_opts.AccessToken) || string.IsNullOrEmpty(_opts.PhoneNumberId))
        {
            _logger.LogWarning("[Meta] AccessToken or PhoneNumberId not configured — message not sent to {To}", toPhoneNumber);
            return;
        }

        var url = $"https://graph.facebook.com/{_opts.ApiVersion}/{_opts.PhoneNumberId}/messages";
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toPhoneNumber,
            type = "text",
            text = new { body = message }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.AccessToken);
        req.Content = JsonContent.Create(payload);

        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("[Meta] Send message failed {Status}: {Body}", resp.StatusCode, body);
        }
    }

    public async Task<(Stream stream, string mimeType)> DownloadMediaAsync(string mediaId, string knownMimeType, CancellationToken ct = default)
    {
        // Step 1: resolve the temporary download URL
        var metaUrl = $"https://graph.facebook.com/{_opts.ApiVersion}/{mediaId}";
        using var urlReq = new HttpRequestMessage(HttpMethod.Get, metaUrl);
        urlReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.AccessToken);
        var urlResp = await _http.SendAsync(urlReq, ct);
        urlResp.EnsureSuccessStatusCode();

        var mediaInfo = await urlResp.Content.ReadFromJsonAsync<MetaMediaInfo>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty media info response from Meta");

        // Step 2: download the actual bytes
        using var dlReq = new HttpRequestMessage(HttpMethod.Get, mediaInfo.Url);
        dlReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.AccessToken);
        var dlResp = await _http.SendAsync(dlReq, HttpCompletionOption.ResponseHeadersRead, ct);
        dlResp.EnsureSuccessStatusCode();

        var mimeType = dlResp.Content.Headers.ContentType?.MediaType ?? knownMimeType;
        var bytes = await dlResp.Content.ReadAsByteArrayAsync(ct);
        return (new MemoryStream(bytes), mimeType);
    }

    private sealed class MetaMediaInfo
    {
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
        [JsonPropertyName("mime_type")] public string MimeType { get; set; } = string.Empty;
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    }
}
