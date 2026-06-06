using AgentSapienxa.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class DocumentUploadConfiguration : IEntityTypeConfiguration<DocumentUpload>
{
    public void Configure(EntityTypeBuilder<DocumentUpload> b)
    {
        b.ToTable("document_uploads");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        b.Property(x => x.FileName).HasColumnName("file_name").IsRequired();
        b.Property(x => x.FileSize).HasColumnName("file_size");
        b.Property(x => x.ContentType).HasColumnName("content_type").IsRequired();
        b.Property(x => x.StoragePath).HasColumnName("storage_path").IsRequired();
        b.Property(x => x.Sha256).HasColumnName("sha256").IsRequired();
        b.Property(x => x.Status).HasColumnName("status").IsRequired();
        b.Property(x => x.ProgressPct).HasColumnName("progress_pct");
        b.Property(x => x.ErrorMessage).HasColumnName("error_message");
        b.Property(x => x.RetryCount).HasColumnName("retry_count");
        b.Property(x => x.ChunkCount).HasColumnName("chunk_count");
        b.Property(x => x.TotalTokens).HasColumnName("total_tokens");
        b.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.StartedAt).HasColumnName("started_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");

        b.HasIndex(x => new { x.CompanyId, x.Sha256 }).IsUnique();
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => x.CompanyId);
    }
}
