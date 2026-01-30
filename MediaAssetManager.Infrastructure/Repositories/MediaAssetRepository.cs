using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Core.Queries;
using MediaAssetManager.Infrastructure.Data;
using MediaAssetManager.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Repositories
{
    public class MediaAssetRepository(MediaAssetContext context) : IMediaAssetRepository
    {
        /// <inheritdoc/>
        public async Task<MediaAsset> AddAsync(MediaAsset asset)
        {
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();
            return asset;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await context.MediaAssets.FindAsync(id);
            if (entity == null) return false;

            context.MediaAssets.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query)
        {
            var baseQuery = context.MediaAssets
                .AsNoTracking()
                .ApplyFilters(query);

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .ApplySorting(query)
                .ApplyPaging(query)
                .ToListAsync();

            return new PagedResult<MediaAsset>(
                items,
                totalCount,
                query.PageNumber,
                query.PageSize);
        }

        /// <inheritdoc/>
        public async Task<MediaAsset?> GetByIdAsync(int id)
        {
            return await context.MediaAssets.FindAsync(id);
        }

        /// <inheritdoc/>
        public async Task<MediaAsset?> UpdateAsync(MediaAsset asset)
        {
            if (!await context.MediaAssets.AnyAsync(x => x.AssetId == asset.AssetId))
                return null;

            context.MediaAssets.Update(asset);
            await context.SaveChangesAsync();
            return asset;
        }
    }
}
