using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly ApplicationDbContext _db;
    public LeadRepository(ApplicationDbContext db) => _db = db;

    public Task<Lead?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct) =>
        _db.Leads.IgnoreQueryFilters().Include(l => l.SalesAgent).FirstOrDefaultAsync(l => l.PhoneNumber == phoneNumber, ct);

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Leads.Include(l => l.SalesAgent).FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task AddAsync(Lead lead, CancellationToken ct)
    {
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Lead lead, CancellationToken ct)
    {
        _db.Leads.Update(lead);
        await _db.SaveChangesAsync(ct);
    }
}
