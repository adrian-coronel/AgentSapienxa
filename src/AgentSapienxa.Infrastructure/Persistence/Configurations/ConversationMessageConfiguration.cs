using AgentSapienxa.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> b)
    {
        b.ToTable("conversation_history");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        b.Property(x => x.Role).HasColumnName("role").IsRequired();
        b.Property(x => x.Content).HasColumnName("content").IsRequired();
        b.Property(x => x.ToolCalls).HasColumnName("tool_calls");
        b.Property(x => x.TokensIn).HasColumnName("tokens_in");
        b.Property(x => x.TokensOut).HasColumnName("tokens_out");
        b.Property(x => x.Model).HasColumnName("model");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasIndex(x => new { x.SessionId, x.CreatedAt });
    }
}
