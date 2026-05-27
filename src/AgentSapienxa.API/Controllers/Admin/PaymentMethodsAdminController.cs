using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/payment-methods")]
[Authorize]
public class PaymentMethodsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PaymentMethodsAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var methods = await _db.PaymentMethods
            .Select(m => new { m.Id, m.Name, m.Description, m.Image, m.LimitAmount })
            .ToListAsync(ct);
        return Ok(methods);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPaymentMethodRequest req, CancellationToken ct)
    {
        var method = AgentSapienxa.Domain.Payments.PaymentMethod.Create(req.Name, req.Description, req.Image, req.LimitAmount);
        _db.PaymentMethods.Add(method);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new { }, new { method.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertPaymentMethodRequest req, CancellationToken ct)
    {
        var method = await _db.PaymentMethods.FindAsync([id], ct);
        if (method is null) return NotFound();

        _db.Entry(method).CurrentValues.SetValues(new
        {
            Name = req.Name,
            Description = req.Description,
            Image = req.Image,
            LimitAmount = req.LimitAmount
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var method = await _db.PaymentMethods.FindAsync([id], ct);
        if (method is null) return NotFound();

        _db.PaymentMethods.Remove(method);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record UpsertPaymentMethodRequest(string Name, string? Description, string? Image, decimal LimitAmount);
