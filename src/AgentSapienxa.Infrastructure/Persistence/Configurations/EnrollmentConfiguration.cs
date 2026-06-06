using AgentSapienxa.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("leads_enrollments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.LeadId).HasColumnName("lead");
        b.Property(x => x.CatalogItemId).HasColumnName("course");
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.Observation).HasColumnName("observation");
        b.Property(x => x.TotalCost).HasColumnName("total_cost").HasColumnType("numeric(18,2)");
        b.Property(x => x.PaymentMethodId).HasColumnName("payment_method");
        b.Property(x => x.Voucher).HasColumnName("voucher");
        b.Property(x => x.SaleAgentId).HasColumnName("sale_agent");
    }
}
