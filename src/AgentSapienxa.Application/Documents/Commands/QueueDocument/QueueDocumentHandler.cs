using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Domain.Documents;
using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.QueueDocument;

public class QueueDocumentHandler : IRequestHandler<QueueDocumentCommand, bool>
{
    private readonly IDocumentUploadRepository _uploads;

    public QueueDocumentHandler(IDocumentUploadRepository uploads) => _uploads = uploads;

    public async Task<bool> Handle(QueueDocumentCommand cmd, CancellationToken ct)
    {
        var upload = await _uploads.GetByIdAsync(cmd.DocumentId, ct);
        if (upload is null || upload.CompanyId != cmd.CompanyId)
            return false;

        // Solo se puede poner en cola si está en un estado "en reposo"
        var queueable = new[] { DocumentStatus.Pending, DocumentStatus.Failed, DocumentStatus.Paused };
        if (!queueable.Contains(upload.Status))
            return false;

        upload.MarkAsQueued();
        await _uploads.UpdateAsync(upload, ct);
        return true;
    }
}
