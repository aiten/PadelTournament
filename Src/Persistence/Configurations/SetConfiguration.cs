using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

using Core.Entities;

public class SetConfiguration : IEntityTypeConfiguration<Set>
{
    public void Configure(EntityTypeBuilder<Set> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasIndex(s => new { s.MatchId, s.No }).IsUnique();

        builder.HasOne(s => s.Match)
            .WithMany(m => m.Sets)
            .HasForeignKey(s => s.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
