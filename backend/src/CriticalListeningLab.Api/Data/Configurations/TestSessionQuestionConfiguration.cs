using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class TestSessionQuestionConfiguration : IEntityTypeConfiguration<TestSessionQuestion>
{
    public void Configure(EntityTypeBuilder<TestSessionQuestion> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.PromptJson).HasColumnType("jsonb").IsRequired();

        builder.Property(q => q.CorrectAnswerKey).HasMaxLength(100).IsRequired();
        builder.Property(q => q.StudentAnswerKey).HasMaxLength(100);

        builder.HasIndex(q => new { q.TestSessionId, q.QuestionIndex }).IsUnique();

        // Token je ulazna tocka za dohvat audija pitanja, pa mora biti
        // jedinstven i indeksiran za lookup.
        builder.HasIndex(q => q.AudioToken).IsUnique();

        // Pitanja su bez smisla bez svoje sesije - jedina Cascade veza
        // prema konfiguracijskim podacima u cijelom modelu.
        builder.HasOne(q => q.TestSession)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.TestSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.AudioAsset)
            .WithMany()
            .HasForeignKey(q => q.AudioAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
