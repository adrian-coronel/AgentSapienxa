using AgentSapienxa.Application.Common.Abstractions;

namespace AgentSapienxa.Infrastructure.Messaging;

public class SimulationMessagingChannel : IMessagingChannel
{
    private readonly List<string> _responses = [];

    public Task SendTextAsync(string recipient, string message, CancellationToken ct = default)
    {
        _responses.Add(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetResponses() => _responses;
}
