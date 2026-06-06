using FluentValidation;

namespace AgentSapienxa.Application.Documents.Commands.UploadDocument;

public class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly HashSet<string> AllowedTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/markdown",
        "text/plain"
    ];

    private static readonly HashSet<string> AllowedExtensions =
        [".pdf", ".docx", ".md", ".txt", ".markdown"];

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public UploadDocumentValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId es requerido.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name =>
            {
                var ext = Path.GetExtension(name).ToLowerInvariant();
                return AllowedExtensions.Contains(ext);
            })
            .WithMessage("Extensión no permitida. Use PDF, DOCX, MD o TXT.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("El archivo está vacío.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage($"El archivo supera el límite de {MaxFileSizeBytes / 1024 / 1024} MB.");

        RuleFor(x => x.ContentType)
            .Must(ct => AllowedTypes.Contains(ct) || IsMdOrTxt(ct))
            .WithMessage("Tipo de contenido no soportado.");
    }

    private static bool IsMdOrTxt(string ct) =>
        ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
}
