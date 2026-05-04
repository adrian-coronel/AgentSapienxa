using AgentSapienxa.Domain.ValueObjects;
using FluentValidation;

namespace AgentSapienxa.Application.Leads.Commands.CaptureLead;

public class CaptureLeadValidator : AbstractValidator<CaptureLeadCommand>
{
    public CaptureLeadValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Must(phone => PhoneNumber.Create(phone).IsSuccess)
            .WithMessage("Formato de teléfono inválido. Use formato E.164 (ej: +51987654321).");
    }
}
