using AgentSapienxa.Domain.Enrollments;

namespace AgentSapienxa.Application.Enrollments.Repositories;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetActiveAsync(Guid leadId, Guid catalogItemId, CancellationToken ct = default);
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetActiveByLeadAsync(Guid leadId, CancellationToken ct = default);
    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);
    Task UpdateAsync(Enrollment enrollment, CancellationToken ct = default);
}
