using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Core.Queries;
using MediaAssetManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MediaAssetManager.Services
{
    public class MediaAssetService : IMediaAssetService
    {
        private readonly IMediaAssetRepository _repository;

        public MediaAssetService(IMediaAssetRepository repository)
        {
            _repository = repository;
        }

        public Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query)
        {
            return _repository.GetAsync(query);
        }

        public Task<MediaAsset?> GetByIdAsync(int id)
        {
            return _repository.GetByIdAsync(id);
        }
    }
}
