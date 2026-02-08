using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Data
{
    /// <summary>
    /// EF Core DbContext for Media Asset Manager
    /// Configured with snake_case for PostgreSQL and comprehensive relationships
    /// </summary>
    public class MediaAssetContext : DbContext
    {
        public MediaAssetContext(DbContextOptions<MediaAssetContext> options)
            : base(options)
        {
        }

        // === DBSETS ===
        public DbSet<MediaAsset> MediaAssets { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<VideoMetadata> VideoMetadata { get; set; } = null!;
        public DbSet<Favorite> Favorites { get; set; } = null!;
        public DbSet<Playlist> Playlists { get; set; } = null!;
        public DbSet<PlaylistItem> PlaylistItems { get; set; } = null!;
        public DbSet<AssetView> AssetViews { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply snake_case naming convention for PostgreSQL
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Table names to snake_case
                var tableName = entity.GetTableName();
                if (tableName != null)
                {
                    entity.SetTableName(tableName.ToSnakeCase());
                }

                // Column names to snake_case
                foreach (var property in entity.GetProperties())
                {
                    var columnName = property.GetColumnName();
                    property.SetColumnName(columnName.ToSnakeCase());
                }
            }

            // ============================================
            // ENTITY CONFIGURATIONS
            // ============================================

            // === USER ===
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.Username);
            });

            // === MEDIA ASSET (Lean Core) ===
            modelBuilder.Entity<MediaAsset>(entity =>
            {
                entity.HasKey(e => e.AssetId);

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.OriginalFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ContentHash)
                    .IsRequired()
                    .HasMaxLength(64); // SHA256 hex string

                // Indexes for common queries
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ContentHash).IsUnique(); // Duplicate detection
                entity.HasIndex(e => e.UploadedAt);
                entity.HasIndex(e => e.GameTitle);
                entity.HasIndex(e => new { e.IsPublic, e.UserId });
                entity.HasIndex(e => e.MediaType);

                // Relationship: MediaAsset -> User
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Assets)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // === VIDEO METADATA (1:0..1) ===
            modelBuilder.Entity<VideoMetadata>(entity =>
            {
                entity.HasKey(e => e.VideoMetadataId);

                entity.HasIndex(e => e.AssetId).IsUnique(); // 1:0..1 enforced
                entity.HasIndex(e => new { e.Width, e.Height }); // Resolution queries

                // Relationship: VideoMetadata -> MediaAsset (1:0..1)
                entity.HasOne(e => e.Asset)
                    .WithOne(a => a.VideoMetadata)
                    .HasForeignKey<VideoMetadata>(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === FAVORITE ===
            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasKey(e => e.FavoriteId);

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
            });

            // === PLAYLIST ===
            modelBuilder.Entity<Playlist>(entity =>
            {
                entity.HasKey(e => e.PlaylistId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.IsPublic });

                // Relationship
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Playlists)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === PLAYLIST ITEM ===
            modelBuilder.Entity<PlaylistItem>(entity =>
            {
                entity.HasKey(e => e.PlaylistItemId);

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
            });

            // === ASSET VIEW (Analytics - High Volume) ===
            modelBuilder.Entity<AssetView>(entity =>
            {
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

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull); // Keep view records if user deleted
            });
        }
    }

    // Extension for snake_case conversion
    public static class StringExtensions
    {
        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return string.Concat(
                input.Select((x, i) => i > 0 && char.IsUpper(x)
                    ? "_" + x.ToString().ToLower()
                    : x.ToString().ToLower())
            );
        }
    }
}
