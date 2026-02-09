using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Enums;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Core.Queries;
using MediaAssetManager.Infrastructure.Data;
using MediaAssetManager.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for MediaAsset data access operations
    /// </summary>
    public class MediaAssetRepository(MediaAssetContext context) : IMediaAssetRepository
    {
        /// <inheritdoc/>
        public async Task<MediaAsset?> GetByIdAsync(int id, HashSet<string>? expand = null)
        {
            var query = context.MediaAssets.AsNoTracking();

            // Apply conditional includes based on expand parameter
            query = ApplyExpands(query, expand);
            return await query.FirstOrDefaultAsync(a => a.AssetId == id);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query)
        {
            var queryable = context.MediaAssets.AsNoTracking();

            // Apply conditional includes based on query.Expand
            queryable = ApplyExpands(queryable, query.Expand);
            // Start with base queryable
            queryable = queryable
                .ApplyFilters(query); // Applies WHERE clauses

            // Get total count BEFORE pagination (for PagedResult)
            var totalCount = await queryable.CountAsync();

            // Apply sorting and pagination, then execute query
            var items = await queryable
                .ApplySorting(query) // Applies ORDER BY
                .ApplyPaging(query) // Applies SKIP/TAKE
                .ToListAsync();

            return new PagedResult<MediaAsset>(
                items,
                totalCount,
                query.PageNumber,
                query.PageSize);
        }

        /// <inheritdoc/>
        public async Task<MediaAsset> AddAsync(MediaAsset asset)
        {
            // Set timestamp automatically
            asset.UploadedAt = DateTime.UtcNow;
            asset.Status = ProcessingStatus.Pending;

            await context.MediaAssets.AddAsync(asset);
            await context.SaveChangesAsync();

            // Reload with navigation properties populated
            return (await GetByIdAsync(asset.AssetId))!;
        }

        /// <inheritdoc/>
        public async Task<MediaAsset?> UpdateAsync(MediaAsset asset)
        {
            // Check if entity exists first (avoid exception)
            var existingAsset = await context.MediaAssets
                .FirstOrDefaultAsync(a => a.AssetId == asset.AssetId);

            if (existingAsset == null)
                return null;

            // Update all properties from incoming entity
            context.Entry(existingAsset).CurrentValues.SetValues(asset);
            await context.SaveChangesAsync();

            // Return updated entity with navigation properties
            return await GetByIdAsync(asset.AssetId);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var asset = await context.MediaAssets.FindAsync(id);

            if (asset == null)
                return false;

            // Cascade delete will remove:
            // - VideoMetadata (1:1)
            // - Favorites (1:N)
            // - PlaylistItems (1:N)
            // - AssetViews (1:N)
            context.MediaAssets.Remove(asset);
            await context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc/>
        public async Task<MediaAsset?> GetByContentHashAsync(string contentHash, HashSet<string>? expand = null)
        {
            // For duplicate detection during upload
            var query = context.MediaAssets.AsNoTracking();

            // Apply conditional includes based on expand parameter
            query = ApplyExpands(query, expand);
            return await query.FirstOrDefaultAsync(a => a.ContentHash == contentHash);
        }

        /// <inheritdoc/>
        public async Task IncrementViewCountAsync(int id)
        {
            // Use ExecuteUpdate for efficient server-side update without loading entity
            await context.MediaAssets
                .Where(a => a.AssetId == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.ViewCount, a => a.ViewCount + 1)
                    .SetProperty(a => a.LastViewedAt, _ => DateTime.UtcNow));

        }

        /// <summary>
        /// Applies conditional eager loading of navigation properties based on expand parameter.
        /// Always includes Views collection for ViewCount calculation.
        /// </summary>
        private static IQueryable<MediaAsset> ApplyExpands(IQueryable<MediaAsset> query, HashSet<string>? expand)
        {
            // Always include Views for count calculation (lightweight, no navigation properties loaded)
            query = query.Include(a => a.Views);

            if (expand == null)
                return query;

            // Conditionally include navigation properties based on expand values (case-insensitive)
            if (expand.Contains("user", StringComparer.OrdinalIgnoreCase))
                query = query.Include(a => a.User);

            if (expand.Contains("videoMetadata", StringComparer.OrdinalIgnoreCase))
                query = query.Include(a => a.VideoMetadata);

            return query;
        }
    }
}