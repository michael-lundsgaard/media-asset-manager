

using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Services.Interfaces
{
    public interface IMediaAssetService
    {
        Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query);
        Task<MediaAsset?> GetByIdAsync(int id);
    }
}
