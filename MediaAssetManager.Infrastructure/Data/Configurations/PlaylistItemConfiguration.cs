using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for PlaylistItem entity
    /// Configures the join table for N:M relationship between Playlist and MediaAsset
    /// </summary>
    public class PlaylistItemConfiguration : IEntityTypeConfiguration<PlaylistItem>
    {
        public void Configure(EntityTypeBuilder<PlaylistItem> entity)
        {
            // Primary Key
            entity.HasKey(e => e.PlaylistItemId);

            // Indexes
            // Unique constraint: can't add same asset to same playlist twice
            entity.HasIndex(e => new { e.PlaylistId, e.AssetId }).IsUnique();
            entity.HasIndex(e => e.AssetId);

            // Relationships
            entity.HasOne(e => e.Playlist)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Asset)
                .WithMany(a => a.PlaylistItems)
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
