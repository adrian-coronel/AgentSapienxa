using AgentSapienxa.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class PaymentValidationConfiguration : IEntityTypeConfiguration<PaymentValidation>
{
    public void Configure(EntityTypeBuilder<PaymentValidation> b)
    {
        b.ToTable("payment_validations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.EnrollmentId).HasColumnName("enrollment_id");
        b.Property(x => x.VoucherDetail).HasColumnName("voucher_detail");
        b.Property(x => x.VoucherUrl).HasColumnName("voucher_url");
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.Observation).HasColumnName("observation");
        b.Property(x => x.RequestedBy).HasColumnName("requested_by");
        b.Property(x => x.ResolvedBy).HasColumnName("resolved_by");
        b.Property(x => x.RequestedAt).HasColumnName("requested_at");
        b.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
    }
}
