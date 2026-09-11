using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class AudioSourceConfiguration : IEntityTypeConfiguration<AudioSource>
{
    public void Configure(EntityTypeBuilder<AudioSource> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Slug).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();

        // Slug je unique unutar modula, ne globalno - "drums" postoji i pod
        // EQ i pod Compression.
        builder.HasIndex(s => new { s.ModuleId, s.Slug }).IsUnique();

        builder.HasOne(s => s.Module)
            .WithMany(m => m.AudioSources)
            .HasForeignKey(s => s.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
