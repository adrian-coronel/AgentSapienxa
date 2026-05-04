using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AgentSapienxa.API.Filters;

public class PaymentsFeatureSwaggerFilter : IDocumentFilter
{
    private readonly IFeatureFlags _flags;
    public PaymentsFeatureSwaggerFilter(IFeatureFlags flags) => _flags = flags;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (_flags.PaymentsEnabled) return;

        var pathsToRemove = context.ApiDescriptions
            .Where(api => api.ActionDescriptor.EndpointMetadata
                .Any(m => m is PaymentsFeatureAttribute))
            .Select(api => "/" + api.RelativePath!.TrimStart('/'))
            .Distinct()
            .ToList();

        foreach (var path in pathsToRemove)
            swaggerDoc.Paths.Remove(path);
    }
}
