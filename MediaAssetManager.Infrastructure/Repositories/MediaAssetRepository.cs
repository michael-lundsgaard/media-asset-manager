using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Core.Queries;
using MediaAssetManager.Infrastructure.Data;
using MediaAssetManager.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Repositories
{
    public class MediaAssetRepository : IMediaAssetRepository
    {
        private readonly MediaAssetContext _context;

        public MediaAssetRepository(MediaAssetContext context)
        {
            _context = context;
        }

        public async Task<MediaAsset> AddAsync(MediaAsset asset)
        {
            _context.MediaAssets.Add(asset);
            await _context.SaveChangesAsync();
            return asset;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.MediaAssets.FindAsync(id);
            if (entity == null) return false;

            _context.MediaAssets.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query)
        {
            var baseQuery = _context.MediaAssets
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
                query.Skip,
                query.Take);
        }

        public async Task<MediaAsset?> GetByIdAsync(int id)
        {
            return await _context.MediaAssets.FindAsync(id);
        }

        public async Task<MediaAsset?> UpdateAsync(MediaAsset asset)
        {
            if (!await _context.MediaAssets.AnyAsync(x => x.AssetId == asset.AssetId))
                return null;

            _context.MediaAssets.Update(asset);
            await _context.SaveChangesAsync();
            return asset;
        }
    }
}
