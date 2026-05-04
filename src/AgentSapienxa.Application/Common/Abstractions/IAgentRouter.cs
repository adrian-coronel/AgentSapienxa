namespace AgentSapienxa.Application.Common.Abstractions;

public interface IAgentRouter
{
    IAgent Route(string intent);
}
