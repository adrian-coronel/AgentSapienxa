using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Documents.Repositories;
using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.DeleteDocument;

public class DeleteDocumentHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IDocumentUploadRepository _uploads;
    private readonly IDocumentChunkRepository _chunks;
    private readonly IFileStorage _storage;

    public DeleteDocumentHandler(
        IDocumentUploadRepository uploads,
        IDocumentChunkRepository chunks,
        IFileStorage storage)
    {
        _uploads = uploads;
        _chunks = chunks;
        _storage = storage;
    }

    public async Task Handle(DeleteDocumentCommand cmd, CancellationToken ct)
    {
        var upload = await _uploads.GetByIdAsync(cmd.DocumentId, ct);
        if (upload is null || upload.CompanyId != cmd.CompanyId)
            return;

        await _chunks.DeleteByDocumentAsync(upload.Id, ct);
        await _storage.DeleteAsync(upload.StoragePath, ct);
        await _uploads.DeleteAsync(upload, ct);
    }
}
