using AgentSapienxa.Domain.Payments;

namespace AgentSapienxa.Application.Payments.Repositories;

public interface IPaymentValidationRepository
{
    Task<PaymentValidation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PaymentValidation validation, CancellationToken ct = default);
    Task UpdateAsync(PaymentValidation validation, CancellationToken ct = default);
}
