using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for Favorite entity
    /// Configures user-asset favorite relationships with unique constraints
    /// </summary>
    public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> entity)
        {
            // Primary Key
            entity.HasKey(e => e.FavoriteId);

            // Indexes
            // Unique constraint: user can't favorite same asset twice
            entity.HasIndex(e => new { e.UserId, e.AssetId }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Asset)
                .WithMany(a => a.Favorites)
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
