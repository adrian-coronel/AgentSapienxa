using AgentSapienxa.Application.Payments.Repositories;
using AgentSapienxa.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly ApplicationDbContext _db;
    public PaymentMethodRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken ct) =>
        await _db.PaymentMethods.ToListAsync(ct);

    public Task<PaymentMethod?> GetByNameAsync(string name, CancellationToken ct) =>
        _db.PaymentMethods.FirstOrDefaultAsync(m => m.Name == name, ct);

    public Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id, ct);
}
