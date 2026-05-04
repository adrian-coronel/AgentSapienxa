using AgentSapienxa.Domain.Agents;

namespace AgentSapienxa.Application.Agents.Repositories;

public interface IAgentConfigRepository
{
    Task<AgentConfig?> GetByKeyAsync(string key, CancellationToken ct = default);
}
