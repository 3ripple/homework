using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShort.Models;

namespace UrlShort.Data.Configurations;

public class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.ToTable("links");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code)
            .IsRequired();
        builder.HasIndex(x => x.Code)
            .IsUnique();
        builder.Property(x => x.OriginalUrl)
            .IsRequired();
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        builder.Property(x => x.Clicks)
            .HasDefaultValue(0);
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();
    }
}