using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class CohortModuleAvailabilityConfiguration : IEntityTypeConfiguration<CohortModuleAvailability>
{
    public void Configure(EntityTypeBuilder<CohortModuleAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        // Najvise jedan override po kombinaciji cohorta i modula.
        builder.HasIndex(a => new { a.CohortId, a.ModuleId }).IsUnique();

        builder.HasOne(a => a.Cohort)
            .WithMany(c => c.ModuleAvailabilities)
            .HasForeignKey(a => a.CohortId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Module)
            .WithMany(m => m.CohortAvailabilities)
            .HasForeignKey(a => a.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
