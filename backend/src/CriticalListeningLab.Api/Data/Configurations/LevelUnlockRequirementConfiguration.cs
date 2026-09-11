using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class LevelUnlockRequirementConfiguration : IEntityTypeConfiguration<LevelUnlockRequirement>
{
    public void Configure(EntityTypeBuilder<LevelUnlockRequirement> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequirementType).HasConversion<int>();

        builder.HasIndex(r => new { r.ExerciseLevelId, r.RequiredExerciseLevelId }).IsUnique();

        // Indeks za obrnuti smjer: "koje levele otkljucava prolaz ovog levela".
        builder.HasIndex(r => r.RequiredExerciseLevelId);

        // Dvije veze prema istoj tablici, pa oba navigacijska svojstva moraju
        // biti eksplicitna - inace EF ne moze razluciti koja je koja.
        builder.HasOne(r => r.ExerciseLevel)
            .WithMany(l => l.UnlockRequirements)
            .HasForeignKey(r => r.ExerciseLevelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RequiredExerciseLevel)
            .WithMany(l => l.UnlocksRequirements)
            .HasForeignKey(r => r.RequiredExerciseLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Level ne moze zahtijevati sam sebe - to bi bio trajno zakljucan level.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_level_unlock_requirement_not_self",
            "exercise_level_id <> required_exercise_level_id"));
    }
}
