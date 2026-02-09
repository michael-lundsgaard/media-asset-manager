using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for AssetView entity
    /// Configures high-volume analytics table with time-series indexes
    /// </summary>
    public class AssetViewConfiguration : IEntityTypeConfiguration<AssetView>
    {
        public void Configure(EntityTypeBuilder<AssetView> entity)
        {
            // Primary Key (long for high volume)
            entity.HasKey(e => e.ViewId);

            // Indexes for analytics queries
            entity.HasIndex(e => e.ViewedAt); // Time-series queries
            entity.HasIndex(e => new { e.AssetId, e.ViewedAt }); // Per-asset analytics
            entity.HasIndex(e => e.UserId); // User view history

            // Relationships
            entity.HasOne(e => e.Asset)
                .WithMany(a => a.Views)
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // SetNull to keep view records if user deleted (preserve analytics)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
