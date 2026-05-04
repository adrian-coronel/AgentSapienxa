namespace AgentSapienxa.Application.Common.Abstractions;

public interface IMessagingChannel
{
    Task SendTextAsync(string recipient, string message, CancellationToken ct = default);
}
