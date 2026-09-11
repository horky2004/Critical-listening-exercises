using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class ExerciseSegmentConfiguration : IEntityTypeConfiguration<ExerciseSegment>
{
    public void Configure(EntityTypeBuilder<ExerciseSegment> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(s => new { s.ModuleId, s.Key }).IsUnique();

        builder.HasOne(s => s.Module)
            .WithMany(m => m.Segments)
            .HasForeignKey(s => s.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
