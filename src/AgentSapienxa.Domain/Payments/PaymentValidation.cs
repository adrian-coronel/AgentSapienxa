using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Payments;

public class PaymentValidation : Entity
{
    public Guid EnrollmentId { get; private set; }
    public string? VoucherDetail { get; private set; }
    public string? VoucherUrl { get; private set; }
    public string Status { get; private set; } = PaymentValidationStatus.Pendiente;
    public string? Observation { get; private set; }
    public string? RequestedBy { get; private set; }
    public string? ResolvedBy { get; private set; }
    public DateTime RequestedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; private set; }

    private PaymentValidation() { }

    public static PaymentValidation Create(Guid enrollmentId, string requestedBy, string? voucherDetail, string? voucherUrl)
    {
        return new PaymentValidation
        {
            EnrollmentId = enrollmentId,
            RequestedBy = requestedBy,
            VoucherDetail = voucherDetail,
            VoucherUrl = voucherUrl,
            Status = PaymentValidationStatus.Pendiente,
            RequestedAt = DateTime.UtcNow
        };
    }

    public Result Resolve(string decision, string resolvedBy, string? observation)
    {
        if (Status != PaymentValidationStatus.Pendiente)
            return Result.Fail("Esta validación ya fue resuelta.");

        if (decision != PaymentValidationStatus.Aprobado && decision != PaymentValidationStatus.Invalido)
            return Result.Fail($"Decisión inválida: {decision}. Use 'Aprobado' o 'Inválido'.");

        Status = decision;
        ResolvedBy = resolvedBy;
        Observation = observation;
        ResolvedAt = DateTime.UtcNow;
        return Result.Ok();
    }
}

public static class PaymentValidationStatus
{
    public const string Pendiente = "Pendiente";
    public const string Aprobado = "Aprobado";
    public const string Invalido = "Inválido";
    public const string Expirado = "Expirado";
}
