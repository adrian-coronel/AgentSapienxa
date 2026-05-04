using AgentSapienxa.Application.Agents;

namespace AgentSapienxa.Application.Common.Abstractions;

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    string JsonSchema { get; }
    Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct = default);
}
