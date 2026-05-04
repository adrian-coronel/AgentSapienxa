using AgentSapienxa.Domain.Catalog;
using AgentSapienxa.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentSapienxa.Infrastructure.Persistence.Configurations;

public class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> b)
    {
        b.ToTable("courses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Code).HasColumnName("code");
        b.Property(x => x.Title).HasColumnName("title").IsRequired();
        b.Property(x => x.ShortDescription).HasColumnName("short_description");
        b.Property(x => x.Features).HasColumnName("features");
        b.Property(x => x.Details).HasColumnName("details");
        b.Property(x => x.Syllabus).HasColumnName("syllabus");
        b.Property(x => x.Projects).HasColumnName("projects");
        b.Property(x => x.Link).HasColumnName("link");
        b.Property(x => x.InstructorId).HasColumnName("instructors");
        b.Property(x => x.Cost).HasColumnName("cost").HasConversion<MoneyConverter>();
        b.Property(x => x.Places).HasColumnName("places");
        b.Property(x => x.AvailablePlaces).HasColumnName("available_places");
        b.Property(x => x.StartDate).HasColumnName("start_date");

        b.HasOne(x => x.Instructor)
            .WithMany()
            .HasForeignKey(x => x.InstructorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
