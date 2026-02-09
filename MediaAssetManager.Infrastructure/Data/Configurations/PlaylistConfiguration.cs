using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for Playlist entity
    /// Configures user-created collections for organizing media assets
    /// </summary>
    public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
    {
        public void Configure(EntityTypeBuilder<Playlist> entity)
        {
            // Primary Key
            entity.HasKey(e => e.PlaylistId);

            // Property Configurations
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
        }
    }
}
