using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Payments;

public class PaymentMethod : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Image { get; private set; }
    public decimal LimitAmount { get; private set; }

    private PaymentMethod() { }
}
