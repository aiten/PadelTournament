using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

using Base.Persistence;
using Core.Entities;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.RowVersion).IsRowVersion();

        builder.HasIndex(g => new { g.SetId, g.No }).IsUnique();

        builder.HasOne(g => g.Set)
            .WithMany(s => s.Games)
            .HasForeignKey(g => g.SetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(g => g.Points).AsRequiredText(100);
    }
}
