using AgentSapienxa.Application.Common.Abstractions;
using System.Security.Claims;

namespace AgentSapienxa.API.Middleware;

public class CompanyResolverMiddleware
{
    private readonly RequestDelegate _next;

    public CompanyResolverMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentCompanyAccessor companyAccessor)
    {
        var companyClaim = context.User.FindFirstValue("company_id");
        if (Guid.TryParse(companyClaim, out var companyId))
            companyAccessor.CompanyId = companyId;

        await _next(context);
    }
}
