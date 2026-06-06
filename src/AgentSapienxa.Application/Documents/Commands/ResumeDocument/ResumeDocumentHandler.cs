using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Domain.Documents;
using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.ResumeDocument;

public class ResumeDocumentHandler : IRequestHandler<ResumeDocumentCommand, bool>
{
    private readonly IDocumentUploadRepository _uploads;

    public ResumeDocumentHandler(IDocumentUploadRepository uploads) => _uploads = uploads;

    public async Task<bool> Handle(ResumeDocumentCommand cmd, CancellationToken ct)
    {
        var upload = await _uploads.GetByIdAsync(cmd.DocumentId, ct);
        if (upload is null || upload.CompanyId != cmd.CompanyId)
            return false;

        if (upload.Status != DocumentStatus.Paused)
            return false;

        upload.MarkAsResumed();
        await _uploads.UpdateAsync(upload, ct);
        return true;
    }
}
