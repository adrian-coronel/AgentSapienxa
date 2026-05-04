namespace AgentSapienxa.Application.Common.Abstractions;

public interface IImageDescriber
{
    Task<string> DescribeAsync(Stream imageStream, string mimeType, CancellationToken ct = default);
}
