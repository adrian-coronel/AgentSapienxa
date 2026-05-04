using AgentSapienxa.Domain.Payments;

namespace AgentSapienxa.Application.Payments.Repositories;

public interface IPaymentMethodRepository
{
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken ct = default);
    Task<PaymentMethod?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<PaymentMethod?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
