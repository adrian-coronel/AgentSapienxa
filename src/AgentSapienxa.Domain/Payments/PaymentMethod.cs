using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Payments;

public class PaymentMethod : Entity
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Image { get; private set; }
    public decimal LimitAmount { get; private set; }

    private PaymentMethod() { }

    public void AssignToCompany(Guid companyId) => CompanyId = companyId;

    public static PaymentMethod Create(string name, string? description, string? image, decimal limitAmount, Guid? companyId = null)
    {
        return new PaymentMethod
        {
            CompanyId = companyId ?? Guid.Empty,
            Name = name,
            Description = description,
            Image = image,
            LimitAmount = limitAmount
        };
    }
}
