namespace AgentSapienxa.Application.Agents;

public class AgentContext
{
    public string SessionId { get; init; } = default!;
    public string ContactName { get; init; } = string.Empty;
    public Guid CompanyId { get; init; }

    public string PhoneE164 => "+" + SessionId.TrimStart('+');
}
