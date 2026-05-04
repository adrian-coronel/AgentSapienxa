using AgentSapienxa.API.Filters;
using AgentSapienxa.Application.Payments.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/payment-methods")]
[PaymentsFeature]
[ServiceFilter(typeof(PaymentsFeatureGate))]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodRepository _methods;
    public PaymentMethodsController(IPaymentMethodRepository methods) => _methods = methods;

    /// <summary>Lista los métodos de pago disponibles.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var methods = await _methods.GetAllAsync(ct);
        return Ok(methods.Select(m => new
        {
            m.Id,
            m.Name,
            m.Description,
            m.Image,
            m.LimitAmount
        }));
    }
}
