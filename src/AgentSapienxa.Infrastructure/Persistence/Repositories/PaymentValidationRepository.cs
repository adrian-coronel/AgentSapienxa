using AgentSapienxa.Application.Payments.Repositories;
using AgentSapienxa.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class PaymentValidationRepository : IPaymentValidationRepository
{
    private readonly ApplicationDbContext _db;
    public PaymentValidationRepository(ApplicationDbContext db) => _db = db;

    public Task<PaymentValidation?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.PaymentValidations.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task AddAsync(PaymentValidation validation, CancellationToken ct)
    {
        _db.PaymentValidations.Add(validation);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PaymentValidation validation, CancellationToken ct)
    {
        _db.PaymentValidations.Update(validation);
        await _db.SaveChangesAsync(ct);
    }
}
