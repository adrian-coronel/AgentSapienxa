using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Leads;

public class SalesAgent : Entity
{
    public string AgentName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? LeadClassificationSummary { get; private set; }

    private SalesAgent() { }
}
