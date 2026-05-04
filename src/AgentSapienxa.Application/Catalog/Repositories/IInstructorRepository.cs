using AgentSapienxa.Domain.Catalog;

namespace AgentSapienxa.Application.Catalog.Repositories;

public interface IInstructorRepository
{
    Task<Instructor?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
