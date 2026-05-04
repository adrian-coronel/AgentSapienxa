using AgentSapienxa.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> b)
    {
        b.ToTable("instructors");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.InstructorName).HasColumnName("instructor_name");
        b.Property(x => x.Email).HasColumnName("email");
        b.Property(x => x.PhoneNumber).HasColumnName("phone_number");
        b.Property(x => x.ProfilePicture).HasColumnName("profile_picture");
        b.Property(x => x.Expertise).HasColumnName("expertise");
        b.Property(x => x.InstructorSummary).HasColumnName("instructor_summary");
    }
}
