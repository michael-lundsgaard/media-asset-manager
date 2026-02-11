using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for MediaAsset entity
    /// Configures the lean core entity with required properties, indexes, and relationships
    /// </summary>
    public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
    {
        public void Configure(EntityTypeBuilder<MediaAsset> entity)
        {
            // Primary Key
            entity.HasKey(e => e.AssetId);

            // Property Configurations
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

            // PostgreSQL jsonb column for flexible Tags storage
            entity.Property(e => e.Tags)
                .HasColumnType("jsonb");

            // Indexes for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ContentHash).IsUnique(); // Duplicate detection
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.GameTitle);
            entity.HasIndex(e => new { e.IsPublic, e.UserId });
            entity.HasIndex(e => e.MediaType);

            // Relationship: MediaAsset -> User
            // SetNull to preserve orphaned records when user is deleted
            entity.HasOne(e => e.User)
                .WithMany(u => u.Assets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ViewCount is managed by database trigger
            entity.Property(e => e.ViewCount)
                .HasDefaultValue(0)
                .ValueGeneratedOnAddOrUpdate() // Prevent EF Core from trying to update this property
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore); // Ignore changes to ViewCount in EF Core since it's managed by the database
        }
    }
}
