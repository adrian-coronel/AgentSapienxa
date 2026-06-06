using AgentSapienxa.Infrastructure.CurrentCompany;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AgentSapienxa.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AgentSapienxa.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connStr = config.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=agentsapienxa;Username=postgres;Password=changeme";

        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connStr, o => o.UseVector())
            .Options;

        // Design-time: no HTTP context, so CompanyId is always null (no filter applied)
        return new ApplicationDbContext(opts, new ScopedCurrentCompanyAccessor());
    }
}
