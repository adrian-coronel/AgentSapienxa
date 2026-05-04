using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Application.Agents;

public class PaymentAgent : GeneralAgent
{
    public PaymentAgent(ILlmProvider llm, IEnumerable<IAgentTool> tools, ILogger<GeneralAgent> logger)
        : base(llm, tools, logger) { }

    protected override bool IsAllowed(IAgentTool tool) => true;
}
