using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Domain.Admin;
using AgentSapienxa.Domain.Agents;
using AgentSapienxa.Domain.Catalog;
using AgentSapienxa.Domain.Companies;
using AgentSapienxa.Domain.Conversations;
using AgentSapienxa.Domain.Documents;
using AgentSapienxa.Domain.Enrollments;
using AgentSapienxa.Domain.Leads;
using AgentSapienxa.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AgentSapienxa.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentCompanyAccessor _company;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentCompanyAccessor company) : base(options)
    {
        _company = company;
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<SalesAgent> SalesAgents => Set<SalesAgent>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<PaymentValidation> PaymentValidations => Set<PaymentValidation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<AgentConfig> AgentConfigs => Set<AgentConfig>();
    public DbSet<DocumentUpload> DocumentUploads => Set<DocumentUpload>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Global query filters — bypass when CompanyId is null (background workers, seeders, super-admin)
        modelBuilder.Entity<Lead>()
            .HasQueryFilter(x => _company.CompanyId == null || x.CompanyId == _company.CompanyId);
        modelBuilder.Entity<CatalogItem>()
            .HasQueryFilter(x => _company.CompanyId == null || x.CompanyId == _company.CompanyId);
        modelBuilder.Entity<ConversationMessage>()
            .HasQueryFilter(x => _company.CompanyId == null || x.CompanyId == _company.CompanyId);
        modelBuilder.Entity<AgentConfig>()
            .HasQueryFilter(x => _company.CompanyId == null || x.CompanyId == _company.CompanyId);
        modelBuilder.Entity<Enrollment>()
            .HasQueryFilter(x => _company.CompanyId == null || x.CompanyId == _company.CompanyId);
        modelBuilder.Entity<DocumentUpload>()
            .HasQueryFilter(x => _company.CompanyId == null || x.CompanyId == _company.CompanyId);
        modelBuilder.Entity<DocumentChunk>()
            .HasQueryFilter(x => _company.CompanyId == null || x.CompanyId == _company.CompanyId);

        base.OnModelCreating(modelBuilder);
    }
}
