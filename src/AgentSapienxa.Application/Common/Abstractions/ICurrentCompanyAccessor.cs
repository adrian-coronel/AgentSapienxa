namespace AgentSapienxa.Application.Common.Abstractions;

public interface ICurrentCompanyAccessor
{
    Guid? CompanyId { get; set; }
}
