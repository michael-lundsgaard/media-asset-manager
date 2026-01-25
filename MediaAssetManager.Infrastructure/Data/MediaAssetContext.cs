using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Data
{
    public class MediaAssetContext : DbContext
    {
        public MediaAssetContext(DbContextOptions<MediaAssetContext> options)
            : base(options)
        {
        }

        public DbSet<MediaAsset> MediaAssets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MediaAsset>(entity =>
            {
                entity.ToTable("media_assets");
                entity.HasKey(e => e.AssetId);
                entity.Property(e => e.AssetId).HasColumnName("asset_id");
                entity.Property(e => e.FileName).HasColumnName("file_name");
                entity.Property(e => e.OriginalFileName).HasColumnName("original_file_name");
                entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
            });
        }
    }
}
