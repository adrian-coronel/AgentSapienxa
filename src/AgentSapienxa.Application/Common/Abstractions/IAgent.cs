using AgentSapienxa.Application.Agents;
using AgentSapienxa.Domain.Agents;

namespace AgentSapienxa.Application.Common.Abstractions;

public interface IAgent
{
    Task<string> RunAsync(
        AgentContext context,
        IReadOnlyList<LlmMessage> history,
        AgentConfig config,
        CancellationToken ct = default);
}
