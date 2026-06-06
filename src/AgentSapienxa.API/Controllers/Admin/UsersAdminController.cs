using AgentSapienxa.Domain.Admin;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "admin,superadmin")]
public class UsersAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UsersAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _db.AdminUsers
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role, u.IsActive, u.CreatedAt, u.CompanyId })
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest req, CancellationToken ct)
    {
        if (await _db.AdminUsers.IgnoreQueryFilters().AnyAsync(u => u.Email == req.Email, ct))
            return Conflict(new { message = "Ya existe un usuario con ese email." });

        var creatorRole = User.FindFirstValue(ClaimTypes.Role);
        var newRole = req.Role ?? AdminRoles.Admin;

        if (!AdminRoles.IsValid(newRole))
            return BadRequest(new { message = $"Rol inválido: '{newRole}'." });

        Guid? targetCompanyId;

        if (creatorRole == AdminRoles.Superadmin)
        {
            if (newRole == AdminRoles.Superadmin)
            {
                targetCompanyId = null;
            }
            else
            {
                if (req.CompanyId is null)
                    return BadRequest(new { message = "Debe especificar una empresa para usuarios admin/editor." });

                var company = await _db.Companies
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == req.CompanyId.Value && c.IsActive, ct);
                if (company is null)
                    return BadRequest(new { message = "Empresa no encontrada o inactiva." });

                targetCompanyId = req.CompanyId;
            }
        }
        else if (creatorRole == AdminRoles.Admin)
        {
            if (newRole == AdminRoles.Superadmin)
                return Forbid();

            var creatorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(creatorIdStr, out var creatorId))
                return Unauthorized();

            var creator = await _db.AdminUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == creatorId, ct);
            if (creator?.CompanyId is null)
                return BadRequest(new { message = "Tu usuario no tiene empresa asignada." });

            targetCompanyId = creator.CompanyId;
        }
        else
        {
            return Forbid();
        }

        AdminUser user;
        try
        {
            user = AdminUser.Create(req.Name, req.Email,
                BCrypt.Net.BCrypt.HashPassword(req.Password), newRole, targetCompanyId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        _db.AdminUsers.Add(user);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new { }, new { user.Id, user.Name, user.Email, user.Role, user.CompanyId });
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

public record CreateAdminUserRequest(string Name, string Email, string Password, string? Role, Guid? CompanyId);
public record UpdateAdminUserRequest(bool IsActive, string? Password);
