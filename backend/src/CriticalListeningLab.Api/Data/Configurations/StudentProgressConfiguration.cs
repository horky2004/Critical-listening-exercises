using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class StudentProgressConfiguration : IEntityTypeConfiguration<StudentProgress>
{
    public void Configure(EntityTypeBuilder<StudentProgress> builder)
    {
        builder.HasKey(p => p.Id);

        // Ovaj kljuc je ono sto cini napredak neovisnim po audio izvoru.
        // Unique indeks ujedno sprjecava da dvije paralelne finalizacije
        // testa naprave dva reda.
        builder.HasIndex(p => new { p.UserId, p.AudioSourceId, p.ExerciseLevelId }).IsUnique();

        // Dohvat cijelog progression treea jednog izvora jednim upitom.
        builder.HasIndex(p => new { p.UserId, p.AudioSourceId });

        builder.HasOne(p => p.User)
            .WithMany(u => u.Progress)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.AudioSource)
            .WithMany(s => s.Progress)
            .HasForeignKey(p => p.AudioSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ExerciseLevel)
            .WithMany(l => l.Progress)
            .HasForeignKey(p => p.ExerciseLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
