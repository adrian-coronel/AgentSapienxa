using AgentSapienxa.Domain.Leads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class SalesAgentConfiguration : IEntityTypeConfiguration<SalesAgent>
{
    public void Configure(EntityTypeBuilder<SalesAgent> b)
    {
        b.ToTable("sales_agents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.AgentName).HasColumnName("agent_name");
        b.Property(x => x.Email).HasColumnName("email");
        b.Property(x => x.PhoneNumber).HasColumnName("phone_number");
        b.Property(x => x.LeadClassificationSummary).HasColumnName("lead_classification_summary");
    }
}
