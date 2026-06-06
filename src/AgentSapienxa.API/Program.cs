using AgentSapienxa.API.Filters;
using AgentSapienxa.API.Middleware;
using AgentSapienxa.Application;
using AgentSapienxa.Domain.Admin;
using AgentSapienxa.Domain.Companies;
using AgentSapienxa.Infrastructure;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no está configurado.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddScoped<PaymentsFeatureGate>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "AgentSapienxa API", Version = "v1" });
    opts.DocumentFilter<PaymentsFeatureSwaggerFilter>();
    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.MigrateAsync();

    // Seed default company
    Guid defaultCompanyId;
    var defaultCompany = db.Companies.FirstOrDefault(c => c.Slug == "default");
    if (defaultCompany is null)
    {
        defaultCompany = Company.Create("Default", "default");
        db.Companies.Add(defaultCompany);
        await db.SaveChangesAsync();

        // Assign all existing records to the default company
        await db.Database.ExecuteSqlRawAsync($@"
            UPDATE leads          SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE courses        SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE conversation_history SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE agent_config   SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE leads_enrollments SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE instructors    SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE sales_agents   SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE payment_methods SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE payment_validations SET company_id = '{defaultCompany.Id}' WHERE company_id = '00000000-0000-0000-0000-000000000000' OR company_id IS NULL;
            UPDATE admin_users    SET company_id = '{defaultCompany.Id}' WHERE company_id IS NULL;
        ");
    }
    defaultCompanyId = defaultCompany.Id;

    await AgentConfigSeeder.SeedAsync(db, seederLogger, defaultCompanyId);

    if (!db.AdminUsers.Any())
    {
        db.AdminUsers.Add(AdminUser.Create(
            "Administrador",
            "admin@agentsapienxa.com",
            BCrypt.Net.BCrypt.HashPassword("Admin@2026"),
            role: AdminRoles.Admin,
            companyId: defaultCompanyId));
        await db.SaveChangesAsync();
    }

    // Multi-tenancy migration: ensure at least one superadmin + one admin for Default company
    var hasSuperadmin = await db.AdminUsers.AnyAsync(u => u.Role == AdminRoles.Superadmin);
    if (!hasSuperadmin)
    {
        var existing = await db.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == "admin@agentsapienxa.com");

        if (existing is not null)
        {
            existing.ChangeRole(AdminRoles.Superadmin, null);
            seederLogger.LogInformation("Promoted {Email} to superadmin", existing.Email);
        }
        else
        {
            db.AdminUsers.Add(AdminUser.Create(
                "Superadmin",
                "superadmin@agentsapienxa.com",
                BCrypt.Net.BCrypt.HashPassword("Admin@2026"),
                role: AdminRoles.Superadmin,
                companyId: null));
            seederLogger.LogInformation("Created seed superadmin superadmin@agentsapienxa.com");
        }

        // Save the superadmin promotion/creation first so the hasDefaultAdmin query
        // reflects the updated state (promoted user is no longer admin for defaultCompany).
        await db.SaveChangesAsync();

        var hasDefaultAdmin = await db.AdminUsers
            .AnyAsync(u => u.CompanyId == defaultCompanyId && u.Role == AdminRoles.Admin);
        if (!hasDefaultAdmin)
        {
            db.AdminUsers.Add(AdminUser.Create(
                "Admin Default",
                "admin-default@agentsapienxa.com",
                BCrypt.Net.BCrypt.HashPassword("Admin@2026"),
                role: AdminRoles.Admin,
                companyId: defaultCompanyId));
            seederLogger.LogInformation("Created seed admin admin-default@agentsapienxa.com for Default company");
            await db.SaveChangesAsync();
        }
    }
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "AgentSapienxa v1"));

app.UseMiddleware<MetaWebhookSignatureMiddleware>();

app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CompanyResolverMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

public partial class Program { }
