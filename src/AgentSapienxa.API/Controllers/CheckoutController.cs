using AgentSapienxa.API.Filters;
using AgentSapienxa.Application.Enrollments.Commands.Checkout;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/checkout")]
[PaymentsFeature]
[ServiceFilter(typeof(PaymentsFeatureGate))]
public class CheckoutController : ControllerBase
{
    private readonly IMediator _mediator;
    public CheckoutController(IMediator mediator) => _mediator = mediator;

    /// <summary>Inicia el proceso de pago para un enrollment activo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CheckoutResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Post([FromBody] CheckoutRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CheckoutCommand(req.PhoneNumber, req.CatalogItemId, req.PaymentMethodName), ct);

        return Ok(new
        {
            status = "ok",
            result.CourseTitle,
            result.Amount,
            paymentMethod = new { result.PaymentMethodName, result.PaymentMethodDescription, result.PaymentMethodImage }
        });
    }
}

public record CheckoutRequest(string PhoneNumber, Guid CatalogItemId, string PaymentMethodName);
