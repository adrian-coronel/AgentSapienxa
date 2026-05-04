using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgentSapienxa.API.Filters;

public class PaymentsFeatureGate : IAsyncActionFilter
{
    private readonly IFeatureFlags _flags;
    public PaymentsFeatureGate(IFeatureFlags flags) => _flags = flags;

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (!_flags.PaymentsEnabled)
        {
            ctx.Result = new NotFoundResult();
            return;
        }
        await next();
    }
}
