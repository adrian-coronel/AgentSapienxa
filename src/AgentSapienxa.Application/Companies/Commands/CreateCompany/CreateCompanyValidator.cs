using FluentValidation;

namespace AgentSapienxa.Application.Companies.Commands.CreateCompany;

public class CreateCompanyValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("El slug solo puede contener letras minúsculas, números y guiones.");
    }
}
