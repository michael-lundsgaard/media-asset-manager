using MediaAssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediaAssetManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity configuration for Client entity
    /// Configures OAuth client credentials for API authentication
    /// </summary>
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> entity)
        {
            // Primary Key
            entity.HasKey(e => e.ClientId);

            // Property Configurations
            entity.Property(e => e.ClientPublicId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ClientSecretHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ClientName)
                .IsRequired()
                .HasMaxLength(100);

            // Indexes
            entity.HasIndex(e => e.ClientPublicId).IsUnique();
            entity.HasIndex(e => e.IsActive);
        }
    }
}
