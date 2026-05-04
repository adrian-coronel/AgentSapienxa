using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _db;
    public EnrollmentRepository(ApplicationDbContext db) => _db = db;

    public Task<Enrollment?> GetActiveAsync(Guid leadId, Guid catalogItemId, CancellationToken ct) =>
        _db.Enrollments.FirstOrDefaultAsync(e =>
            e.LeadId == leadId &&
            e.CatalogItemId == catalogItemId &&
            EnrollmentStatus.ActiveStatuses.Contains(e.Status), ct);

    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Enrollment>> GetActiveByLeadAsync(Guid leadId, CancellationToken ct) =>
        await _db.Enrollments
            .Where(e => e.LeadId == leadId && EnrollmentStatus.ActiveStatuses.Contains(e.Status))
            .ToListAsync(ct);

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Enrollment enrollment, CancellationToken ct)
    {
        _db.Enrollments.Update(enrollment);
        await _db.SaveChangesAsync(ct);
    }
}
