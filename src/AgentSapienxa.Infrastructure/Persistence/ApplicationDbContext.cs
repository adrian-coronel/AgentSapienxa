using AgentSapienxa.Domain.Agents;
using AgentSapienxa.Domain.Catalog;
using AgentSapienxa.Domain.Conversations;
using AgentSapienxa.Domain.Enrollments;
using AgentSapienxa.Domain.Leads;
using AgentSapienxa.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AgentSapienxa.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<SalesAgent> SalesAgents => Set<SalesAgent>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<PaymentValidation> PaymentValidations => Set<PaymentValidation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<AgentConfig> AgentConfigs => Set<AgentConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
