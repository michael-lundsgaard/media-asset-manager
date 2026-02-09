using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Infrastructure.Data;
using MediaAssetManager.Infrastructure.Repositories;
using MediaAssetManager.Services;
using MediaAssetManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MediaAssetManager.API.Configuration
{
    /// <summary>
    /// Extension methods for configuring services in the DI container
    /// Keeps Program.cs clean and organized
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all application services (business logic layer)
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IMediaAssetService, MediaAssetService>();
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IStorageService, B2StorageService>();

            // TODO: Add these as you implement them
            // services.AddScoped<IVideoMetadataService, VideoMetadataService>();
            // services.AddScoped<IThumbnailService, ThumbnailService>();
            // services.AddScoped<IVideoCompressionService, VideoCompressionService>();
            // services.AddScoped<IVideoProcessingService, VideoProcessingService>();
            // services.AddScoped<IAuthenticationService, AuthenticationService>();

            return services;
        }

        /// <summary>
        /// Registers all repository implementations (data access layer)
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
            services.AddScoped<IPlaylistRepository, PlaylistRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            // services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            // services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

            return services;
        }

        /// <summary>
        /// Configures PostgreSQL database with connection string from configuration
        /// Enables dynamic JSON serialization for jsonb columns
        /// </summary>
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Build NpgsqlDataSource with dynamic JSON support
            var dataSource = new NpgsqlDataSourceBuilder(connectionString)
                .EnableDynamicJson()
                .Build();

            services.AddDbContext<MediaAssetContext>(options =>
                options.UseNpgsql(dataSource)
                       .UseSnakeCaseNamingConvention());

            return services;
        }

        /// <summary>
        /// Registers HTTP client factory for external API calls
        /// </summary>
        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient();
            return services;
        }
    }
}
