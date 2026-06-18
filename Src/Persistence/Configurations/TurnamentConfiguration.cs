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

        builder.Property(j => j.Description).AsRequiredText(200);
    }
}