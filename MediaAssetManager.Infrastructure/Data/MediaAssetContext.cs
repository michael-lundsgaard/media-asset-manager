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
        public DbSet<Client> Clients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from separate configuration classes
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(MediaAssetContext).Assembly);
        }
    }
}
