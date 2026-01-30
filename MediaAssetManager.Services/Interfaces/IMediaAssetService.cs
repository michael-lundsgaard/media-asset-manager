

using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for media asset business logic operations.
    /// </summary>
    public interface IMediaAssetService
    {
        /// <summary>
        /// Retrieves a paged list of media assets based on the specified query criteria.
        /// </summary>
        /// <param name="query">The query criteria for filtering, sorting, and pagination.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result of media assets.</returns>
        Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query);

        /// <summary>
        /// Retrieves a media asset by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the media asset if found; otherwise, null.</returns>
        Task<MediaAsset?> GetByIdAsync(int id);
    }
}
