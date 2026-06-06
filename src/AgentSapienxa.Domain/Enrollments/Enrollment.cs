using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Enrollments;

public class Enrollment : Entity
{
    public Guid CompanyId { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid CatalogItemId { get; private set; }
    public string Status { get; private set; } = EnrollmentStatus.Interesado;
    public string? Observation { get; private set; }
    public decimal? TotalCost { get; private set; }
    public Guid? PaymentMethodId { get; private set; }
    public string? Voucher { get; private set; }
    public Guid? SaleAgentId { get; private set; }

    private Enrollment() { }

    public void AssignToCompany(Guid companyId) => CompanyId = companyId;

    public static Enrollment Create(Guid leadId, Guid catalogItemId, Guid? companyId = null)
    {
        return new Enrollment
        {
            CompanyId = companyId ?? Guid.Empty,
            LeadId = leadId,
            CatalogItemId = catalogItemId,
            Status = EnrollmentStatus.Interesado
        };
    }

    public Result Transition(string newStatus)
    {
        if (!EnrollmentStatus.CanTransitionTo(Status, newStatus))
            return Result.Fail($"No se puede pasar de '{Status}' a '{newStatus}'.");
        Status = newStatus;
        return Result.Ok();
    }

    public void SetPayment(Guid paymentMethodId, decimal totalCost)
    {
        PaymentMethodId = paymentMethodId;
        TotalCost = totalCost;
    }

    public void SetVoucher(string voucherUrl)
    {
        Voucher = voucherUrl;
    }

    public void Escalate(Guid salesAgentId, string? observation = null)
    {
        Status = EnrollmentStatus.EscaladoAHumano;
        SaleAgentId = salesAgentId;
        Observation = observation;
    }
}
