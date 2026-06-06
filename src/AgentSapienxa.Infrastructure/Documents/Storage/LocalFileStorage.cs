using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AgentSapienxa.Infrastructure.Documents.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IConfiguration config)
    {
        _basePath = config["Documents:StoragePath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "uploads");
    }

    public async Task<string> SaveAsync(Stream content, string companyId, string fileId, string extension, CancellationToken ct = default)
    {
        var dir = Path.Combine(_basePath, companyId);
        Directory.CreateDirectory(dir);

        var fileName = $"{fileId}{extension}";
        var fullPath = Path.Combine(dir, fileName);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        return fullPath;
    }

    public Task<Stream> OpenAsync(string storagePath, CancellationToken ct = default)
    {
        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        if (File.Exists(storagePath))
            File.Delete(storagePath);
        return Task.CompletedTask;
    }
}
