using AgentSapienxa.Domain.Leads;

namespace AgentSapienxa.Application.Leads.Repositories;

public interface ILeadRepository
{
    Task<Lead?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
}
