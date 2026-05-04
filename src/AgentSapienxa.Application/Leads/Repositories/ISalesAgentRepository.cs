using AgentSapienxa.Domain.Leads;

namespace AgentSapienxa.Application.Leads.Repositories;

public interface ISalesAgentRepository
{
    Task<SalesAgent?> GetFirstAvailableAsync(CancellationToken ct = default);
    Task<SalesAgent?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
