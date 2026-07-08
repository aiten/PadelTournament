using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

using Base.Persistence;

using Persistence.Model;

public class FormatConfiguration : IEntityTypeConfiguration<Format>
{
    public void Configure(EntityTypeBuilder<Format> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.RowVersion).IsRowVersion();

        builder.HasIndex(f => f.Name).IsUnique();

        builder.Property(f => f.Name).AsRequiredText(100);
    }
}
