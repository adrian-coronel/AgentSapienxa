using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AuthAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthAdminController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas." });

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        if (user.CompanyId.HasValue)
        {
            var company = await _db.Companies.FindAsync([user.CompanyId.Value], ct);
            claims.Add(new Claim("company_id", user.CompanyId.Value.ToString()));
            if (company is not null)
                claims.Add(new Claim("company_name", company.Name));
        }

        var jwtToken = BuildToken(claims);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            expiresAt = jwtToken.ValidTo,
            user = new { user.Id, user.Name, user.Email, user.Role }
        });
    }

    [HttpPost("switch-company")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "superadmin")]
    public async Task<IActionResult> SwitchCompany([FromBody] SwitchCompanyRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.AdminUsers.FindAsync([userId], ct);
        if (user is null) return NotFound();

        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == req.CompanyId && c.IsActive, ct);
        if (company is null) return NotFound(new { message = "Empresa no encontrada o inactiva." });

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("company_id", company.Id.ToString()),
            new Claim("company_name", company.Name)
        };

        var jwtToken = BuildToken(claims);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            expiresAt = jwtToken.ValidTo,
            companyId = company.Id,
            companyName = company.Name
        });
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.AdminUsers.FindAsync([userId], ct);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Name, user.Email, user.Role });
    }

    private JwtSecurityToken BuildToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado.")));

        return new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
    }
}

public record LoginRequest(string Email, string Password);
public record SwitchCompanyRequest(Guid CompanyId);
