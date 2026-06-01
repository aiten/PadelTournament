using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

using Base.Persistence;
using Core.Entities;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasIndex(m => new { m.TournamentId, m.No, m.Round }).IsUnique();

        builder.HasOne(m => m.TeamA)
            .WithMany()
            .HasForeignKey(m => m.TeamAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.TeamB)
            .WithMany()
            .HasForeignKey(m => m.TeamBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.NextMatch)
            .WithMany()
            .HasForeignKey(m => m.NextMatchId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Tournament)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Remark).AsText(200);
    }
}
