using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for User entity
    /// Configures user account properties and indexes
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            // Primary Key
            entity.HasKey(e => e.UserId);

            // Property Configurations
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);

            // Indexes
            entity.HasIndex(e => e.Username);
        }
    }
}
