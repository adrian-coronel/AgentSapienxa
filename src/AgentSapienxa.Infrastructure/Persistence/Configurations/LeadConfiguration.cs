using AgentSapienxa.Domain.Leads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> b)
    {
        b.ToTable("leads");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.LeadName).HasColumnName("lead_name");
        b.Property(x => x.Email).HasColumnName("email");
        b.Property(x => x.PhoneNumber).HasColumnName("phone_number").IsRequired();
        b.Property(x => x.ContactMethod).HasColumnName("contact_method");
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.SalesAgentId).HasColumnName("sales_agent");

        b.HasIndex(x => x.PhoneNumber).IsUnique();

        b.HasOne(x => x.SalesAgent)
            .WithMany()
            .HasForeignKey(x => x.SalesAgentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
