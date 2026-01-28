using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Core.Interfaces
{
    public interface IMediaAssetRepository
    {
        Task<MediaAsset?> GetByIdAsync(int id);
        Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query);
        Task<MediaAsset> AddAsync(MediaAsset asset);
        Task<MediaAsset?> UpdateAsync(MediaAsset asset);
        Task<bool> DeleteAsync(int id);

    }
}
