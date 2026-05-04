using AgentSapienxa.Domain.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class AgentConfigConfiguration : IEntityTypeConfiguration<AgentConfig>
{
    public void Configure(EntityTypeBuilder<AgentConfig> b)
    {
        b.ToTable("agent_config");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.AgentKey).HasColumnName("agent_key").IsRequired();
        b.Property(x => x.Name).HasColumnName("name").IsRequired();
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.SystemPrompt).HasColumnName("system_prompt").IsRequired();
        b.Property(x => x.Model).HasColumnName("model").IsRequired();
        b.Property(x => x.Temperature).HasColumnName("temperature");
        b.Property(x => x.MaxTokens).HasColumnName("max_tokens");
        b.Property(x => x.MemoryWindow).HasColumnName("memory_window");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(x => x.AgentKey).IsUnique();
    }
}
