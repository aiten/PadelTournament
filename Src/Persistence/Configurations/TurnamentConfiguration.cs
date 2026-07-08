using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

using Base.Persistence;

using Persistence.Model;

public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.RowVersion).IsRowVersion();

        builder.HasIndex(t => t.RegistrationPin).IsUnique();

        builder.Property(j => j.RegistrationPin).AsRequiredText(5);
        builder.Property(j => j.Description).AsRequiredText(200);
        builder.Property(j => j.UserId).HasMaxLength(256);

        builder.HasOne(t => t.Format)
            .WithMany(f => f.Tournaments)
            .HasForeignKey(t => t.FormatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}