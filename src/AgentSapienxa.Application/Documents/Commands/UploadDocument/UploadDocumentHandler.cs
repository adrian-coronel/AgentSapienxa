using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Domain.Documents;
using MediatR;
using System.Security.Cryptography;

namespace AgentSapienxa.Application.Documents.Commands.UploadDocument;

public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, UploadDocumentResult>
{
    private readonly IDocumentUploadRepository _uploads;
    private readonly IFileStorage _storage;

    public UploadDocumentHandler(IDocumentUploadRepository uploads, IFileStorage storage)
    {
        _uploads = uploads;
        _storage = storage;
    }

    public async Task<UploadDocumentResult> Handle(UploadDocumentCommand cmd, CancellationToken ct)
    {
        // Compute SHA-256 for deduplication
        string sha256;
        using (var ms = new MemoryStream())
        {
            await cmd.FileContent.CopyToAsync(ms, ct);
            ms.Position = 0;
            sha256 = Convert.ToHexString(await SHA256.HashDataAsync(ms, ct)).ToLowerInvariant();
            ms.Position = 0;

            // Check for duplicate
            var existing = await _uploads.GetByCompanyAndSha256Async(cmd.CompanyId, sha256, ct);
            if (existing is not null)
                return new UploadDocumentResult(existing.Id, existing.Status);

            var ext = Path.GetExtension(cmd.FileName).ToLowerInvariant();
            var fileId = Guid.NewGuid().ToString();
            var storagePath = await _storage.SaveAsync(ms, cmd.CompanyId.ToString(), fileId, ext, ct);

            var upload = DocumentUpload.Create(
                companyId: cmd.CompanyId,
                fileName: cmd.FileName,
                fileSize: cmd.FileSize,
                contentType: cmd.ContentType,
                storagePath: storagePath,
                sha256: sha256,
                uploadedBy: cmd.UploadedBy);

            await _uploads.AddAsync(upload, ct);
            return new UploadDocumentResult(upload.Id, upload.Status);
        }
    }
}
