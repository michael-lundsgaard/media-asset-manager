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


            modelBuilder.Entity<MediaAsset>(entity =>
            {
                entity.HasKey(e => e.AssetId);
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
