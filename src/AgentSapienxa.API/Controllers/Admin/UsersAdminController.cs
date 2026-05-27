using AgentSapienxa.Domain.Admin;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "admin")]
public class UsersAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UsersAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _db.AdminUsers
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role, u.IsActive, u.CreatedAt })
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest req, CancellationToken ct)
    {
        if (await _db.AdminUsers.AnyAsync(u => u.Email == req.Email, ct))
            return Conflict(new { message = "Ya existe un usuario con ese email." });

        var user = AdminUser.Create(req.Name, req.Email,
            BCrypt.Net.BCrypt.HashPassword(req.Password), req.Role ?? "admin");

        _db.AdminUsers.Add(user);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new { }, new { user.Id, user.Name, user.Email, user.Role });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminUserRequest req, CancellationToken ct)
    {
        var user = await _db.AdminUsers.FindAsync([id], ct);
        if (user is null) return NotFound();

        user.SetActive(req.IsActive);
        if (!string.IsNullOrWhiteSpace(req.Password))
            user.UpdatePassword(BCrypt.Net.BCrypt.HashPassword(req.Password));

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CreateAdminUserRequest(string Name, string Email, string Password, string? Role);
public record UpdateAdminUserRequest(bool IsActive, string? Password);
