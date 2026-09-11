using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class TestSessionConfiguration : IEntityTypeConfiguration<TestSession>
{
    public void Configure(EntityTypeBuilder<TestSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<int>();

        // Postotak je izvedeno svojstvo i ne mapira se u kolonu.
        builder.Ignore(s => s.ScorePercentage);

        // Dohvat aktivne sesije i povijesti pokusaja.
        builder.HasIndex(s => new { s.UserId, s.ExerciseLevelId, s.AudioSourceId, s.Status });

        builder.HasOne(s => s.User)
            .WithMany(u => u.TestSessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.AudioSource)
            .WithMany(a => a.TestSessions)
            .HasForeignKey(s => s.AudioSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ExerciseLevel)
            .WithMany(l => l.TestSessions)
            .HasForeignKey(s => s.ExerciseLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
