using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Leads;

public class Lead : Entity
{
    public Guid CompanyId { get; private set; }
    public string? LeadName { get; private set; }
    public string? Email { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? ContactMethod { get; private set; }
    public string Status { get; private set; } = LeadStatus.New;
    public Guid? SalesAgentId { get; private set; }

    public SalesAgent? SalesAgent { get; private set; }

    private Lead() { }

    public static Lead Create(string phoneNumber, string? name = null, string? email = null, string? contactMethod = null, Guid? companyId = null)
    {
        return new Lead
        {
            CompanyId = companyId ?? Guid.Empty,
            PhoneNumber = phoneNumber,
            LeadName = name,
            Email = email,
            ContactMethod = contactMethod,
            Status = LeadStatus.New
        };
    }

    public void AssignToCompany(Guid companyId) => CompanyId = companyId;

    public void UpdateInfo(string? name, string? email)
    {
        if (!string.IsNullOrWhiteSpace(name) && name != LeadName)
            LeadName = name;
        if (!string.IsNullOrWhiteSpace(email) && email != Email)
            Email = email;
    }

    public void AssignAgent(Guid salesAgentId)
    {
        SalesAgentId = salesAgentId;
    }
}
