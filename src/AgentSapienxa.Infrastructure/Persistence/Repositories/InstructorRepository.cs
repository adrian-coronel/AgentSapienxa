using AgentSapienxa.Application.Catalog.Repositories;
using AgentSapienxa.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class InstructorRepository : IInstructorRepository
{
    private readonly ApplicationDbContext _db;
    public InstructorRepository(ApplicationDbContext db) => _db = db;

    public Task<Instructor?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Instructors.FirstOrDefaultAsync(i => i.Id == id, ct);
}
