namespace AgentSapienxa.Application.Common.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string companyId, string fileId, string extension, CancellationToken ct = default);
    Task<Stream> OpenAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
