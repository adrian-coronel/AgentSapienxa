using AgentSapienxa.Application.Documents.Repositories;
using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.QueueAllDocuments;

public class QueueAllDocumentsHandler : IRequestHandler<QueueAllDocumentsCommand, int>
{
    private readonly IDocumentUploadRepository _uploads;

    public QueueAllDocumentsHandler(IDocumentUploadRepository uploads) => _uploads = uploads;

    public Task<int> Handle(QueueAllDocumentsCommand cmd, CancellationToken ct) =>
        _uploads.QueueAllPendingByCompanyAsync(cmd.CompanyId, ct);
}
