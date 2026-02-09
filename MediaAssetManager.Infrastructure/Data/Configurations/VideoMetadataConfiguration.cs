using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for VideoMetadata entity
    /// Configures the 1:0..1 relationship with MediaAsset and resolution indexes
    /// </summary>
    public class VideoMetadataConfiguration : IEntityTypeConfiguration<VideoMetadata>
    {
        public void Configure(EntityTypeBuilder<VideoMetadata> entity)
        {
            // Primary Key
            entity.HasKey(e => e.VideoMetadataId);

            // Indexes
            entity.HasIndex(e => e.AssetId).IsUnique(); // 1:0..1 enforced
            entity.HasIndex(e => new { e.Width, e.Height }); // Resolution queries

            // Relationship: VideoMetadata -> MediaAsset (1:0..1)
            entity.HasOne(e => e.Asset)
                .WithOne(a => a.VideoMetadata)
                .HasForeignKey<VideoMetadata>(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
