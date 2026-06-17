using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

using Base.Persistence;

using Persistence.Model;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.TournamentId, t.Player1, t.Player2 }).IsUnique();

        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasOne(t => t.Tournament)
            .WithMany(t => t.Teams)
            .HasForeignKey(t => t.TournamentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Player1).AsRequiredText(64);
        builder.Property(t => t.Player2).AsText(64);
        builder.Ignore(t => t.Name);

        builder.Property(j => j.RegistrationCode).AsText(5);
    }
}
