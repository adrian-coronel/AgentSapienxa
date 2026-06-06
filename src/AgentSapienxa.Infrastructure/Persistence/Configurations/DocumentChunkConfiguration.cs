using AgentSapienxa.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> b)
    {
        b.ToTable("document_chunks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DocumentId).HasColumnName("document_id").IsRequired();
        b.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        b.Property(x => x.ChunkIndex).HasColumnName("chunk_index");
        b.Property(x => x.HeaderPath).HasColumnName("header_path");
        b.Property(x => x.Content).HasColumnName("content").IsRequired();
        b.Property(x => x.TokenCount).HasColumnName("token_count");
        b.Property(x => x.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("vector(768)")
            .HasConversion(
                v => new Vector(v),
                v => v.ToArray())
            .IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasIndex(x => x.CompanyId);
        b.HasIndex(x => x.DocumentId);
    }
}
