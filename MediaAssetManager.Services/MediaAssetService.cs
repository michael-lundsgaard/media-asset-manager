using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Core.Queries;
using MediaAssetManager.Services.Interfaces;

namespace MediaAssetManager.Services
{
    public class MediaAssetService(IMediaAssetRepository repository) : IMediaAssetService
    {
        /// <inheritdoc/>
        public Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query)
        {
            return repository.GetAsync(query);
        }

        /// <inheritdoc/>
        public Task<MediaAsset?> GetByIdAsync(int id)
        {
            return repository.GetByIdAsync(id);
        }
    }
}
