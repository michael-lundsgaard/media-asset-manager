using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MediaAssetManager.Infrastructure.Data
{
    /// <summary>
    /// Design-time factory for creating MediaAssetContext instances during EF Core migrations.
    /// Enables EF Core tools (migrations, database update) to access configuration from:
    /// - User Secrets (local development)
    /// - Environment Variables (production/CI/CD)
    /// </summary>
    public class MediaAssetContextFactory : IDesignTimeDbContextFactory<MediaAssetContext>
    {
        public MediaAssetContext CreateDbContext(string[] args)
        {
            // Build configuration with sources added in priority order (last wins):
            // 1. User Secrets (for local dev - shared UserSecretsId with API project)
            // 2. Environment Variables (for production - highest priority)
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(MediaAssetContext).Assembly, optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found.\n\n" +
                    "For local development, configure user secrets:\n" +
                    "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<connection-string>\" --project MediaAssetManager.API\n\n" +
                    "For production, set environment variable:\n" +
                    "  ConnectionStrings__DefaultConnection=<connection-string>\n\n" +
                    "See CONFIGURATION.md for details.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<MediaAssetContext>();
            optionsBuilder.UseNpgsql(connectionString)
                          .UseSnakeCaseNamingConvention();

            return new MediaAssetContext(optionsBuilder.Options);
        }
    }
}
