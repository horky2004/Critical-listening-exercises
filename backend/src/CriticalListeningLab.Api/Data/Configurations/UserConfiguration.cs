using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CriticalListeningLab.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.EntraObjectId).HasMaxLength(64).IsRequired();
        builder.HasIndex(u => u.EntraObjectId).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();

        builder.Property(u => u.Role).HasConversion<int>();

        builder.HasOne(u => u.Cohort)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CohortId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
