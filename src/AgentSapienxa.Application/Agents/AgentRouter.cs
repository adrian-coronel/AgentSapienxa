using AgentSapienxa.Application.Common.Abstractions;

namespace AgentSapienxa.Application.Agents;

public class AgentRouter : IAgentRouter
{
    private readonly GeneralAgent _general;
    private readonly PaymentAgent _payment;
    private readonly IFeatureFlags _flags;

    public AgentRouter(GeneralAgent general, PaymentAgent payment, IFeatureFlags flags)
    {
        _general = general;
        _payment = payment;
        _flags = flags;
    }

    public IAgent Route(string intent)
    {
        if (_flags.PaymentsEnabled && intent == Intent.Payment)
            return _payment;

        return _general;
    }
}
