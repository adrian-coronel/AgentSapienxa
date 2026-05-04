using System.Security.Cryptography;
using System.Text;

namespace AgentSapienxa.API.Middleware;

public class MetaWebhookSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<MetaWebhookSignatureMiddleware> _logger;

    public MetaWebhookSignatureMiddleware(RequestDelegate next, IConfiguration config, ILogger<MetaWebhookSignatureMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ctx.Request.Path.StartsWithSegments("/api/webhooks/whatsapp") || ctx.Request.Method != HttpMethods.Post)
        {
            await _next(ctx);
            return;
        }

        ctx.Request.EnableBuffering();
        var body = await new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
        ctx.Request.Body.Position = 0;

        var signature = ctx.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        var appSecret = _config["Meta:AppSecret"];

        if (string.IsNullOrEmpty(appSecret))
        {
            _logger.LogWarning("Meta:AppSecret not configured, skipping webhook signature validation.");
            await _next(ctx);
            return;
        }

        if (string.IsNullOrEmpty(signature) || !IsValidSignature(body, signature, appSecret))
        {
            _logger.LogWarning("Invalid webhook signature from {Ip}", ctx.Connection.RemoteIpAddress);
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(ctx);
    }

    private static bool IsValidSignature(string body, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        return string.Equals(signature, expected, StringComparison.OrdinalIgnoreCase);
    }
}
