using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Domain.Documents;
using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.PauseDocument;

public class PauseDocumentHandler : IRequestHandler<PauseDocumentCommand, bool>
{
    private readonly IDocumentUploadRepository _uploads;

    public PauseDocumentHandler(IDocumentUploadRepository uploads) => _uploads = uploads;

    public async Task<bool> Handle(PauseDocumentCommand cmd, CancellationToken ct)
    {
        var upload = await _uploads.GetByIdAsync(cmd.DocumentId, ct);
        if (upload is null || upload.CompanyId != cmd.CompanyId)
            return false;

        var pauseable = new[] { DocumentStatus.Pending, DocumentStatus.Parsing, DocumentStatus.Chunking, DocumentStatus.Embedding };
        if (!pauseable.Contains(upload.Status))
            return false;

        upload.MarkAsPaused();
        await _uploads.UpdateAsync(upload, ct);
        return true;
    }
}
