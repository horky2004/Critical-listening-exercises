using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class CohortConfiguration : IEntityTypeConfiguration<Cohort>
{
    public void Configure(EntityTypeBuilder<Cohort> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();

        // Aktivan cohort moze biti samo jedan - filtrirani unique indeks to
        // osigurava na razini baze, pa dodjela cohorta pri prijavi nije dvosmislena.
        builder.HasIndex(c => c.IsActive)
            .IsUnique()
            .HasFilter("is_active");
    }
}
