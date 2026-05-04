using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Infrastructure.Messaging;

public class NullMessagingChannel : IMessagingChannel
{
    private readonly ILogger<NullMessagingChannel> _logger;
    public NullMessagingChannel(ILogger<NullMessagingChannel> logger) => _logger = logger;

    public Task SendTextAsync(string recipient, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("[NullMessaging] → {Recipient}: {Message}", recipient, message);
        return Task.CompletedTask;
    }
}
