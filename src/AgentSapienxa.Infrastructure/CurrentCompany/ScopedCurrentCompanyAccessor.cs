using AgentSapienxa.Application.Common.Abstractions;

namespace AgentSapienxa.Infrastructure.CurrentCompany;

public class ScopedCurrentCompanyAccessor : ICurrentCompanyAccessor
{
    public Guid? CompanyId { get; set; }
}
