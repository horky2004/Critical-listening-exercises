using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class AudioAssetConfiguration : IEntityTypeConfiguration<AudioAsset>
{
    public void Configure(EntityTypeBuilder<AudioAsset> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.VariantSlug).HasMaxLength(50).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(50).IsRequired();

        builder.HasIndex(a => new { a.AudioSourceId, a.VariantSlug }).IsUnique();

        builder.HasOne(a => a.AudioSource)
            .WithMany(s => s.Assets)
            .HasForeignKey(a => a.AudioSourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
