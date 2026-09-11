using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class ExerciseLevelConfiguration : IEntityTypeConfiguration<ExerciseLevel>
{
    public void Configure(EntityTypeBuilder<ExerciseLevel> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title).HasMaxLength(100).IsRequired();

        builder.Property(l => l.ExerciseType).HasConversion<int>();

        builder.Property(l => l.ConfigJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(l => new { l.SegmentId, l.LevelNumber }).IsUnique();

        builder.HasOne(l => l.Segment)
            .WithMany(s => s.Levels)
            .HasForeignKey(l => l.SegmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
