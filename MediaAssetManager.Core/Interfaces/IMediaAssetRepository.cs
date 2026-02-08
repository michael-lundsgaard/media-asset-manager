using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for media asset data access operations.
    /// </summary>
    public interface IMediaAssetRepository
    {
        /// <summary>
        /// Retrieves a media asset by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the media asset.</param>
        /// <param name="includeRelated">Whether to include related entities (user, video metadata). Default is false for performance.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the media asset if found; otherwise, null.</returns>
        Task<MediaAsset?> GetByIdAsync(int id, bool includeRelated = false);

        /// <summary>
        /// Retrieves a paged list of media assets based on the specified query criteria.
        /// </summary>
        /// <param name="query">The query criteria for filtering, sorting, and pagination.</param>
        /// <param name="includeRelated">Whether to include related entities (user, video metadata). Default is false for performance.</param>
        Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query, bool includeRelated = false);

        /// <summary>
        /// Adds a new media asset to the repository.
        /// </summary>
        /// <param name="asset">The media asset to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added media asset with updated properties (e.g., generated ID).</returns>
        Task<MediaAsset> AddAsync(MediaAsset asset);

        /// <summary>
        /// Updates an existing media asset in the repository.
        /// </summary>
        /// <param name="asset">The media asset with updated values.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated media asset if found; otherwise, null.</returns>
        Task<MediaAsset?> UpdateAsync(MediaAsset asset);

        /// <summary>
        /// Deletes a media asset from the repository by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the media asset to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the asset was deleted; otherwise, false.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Finds a media asset by its content hash (for duplicate detection).
        /// </summary>
        /// <param name="contentHash">The SHA256 hash of the file content.</param>
        /// <param name="includeRelated">Whether to include related entities (user, video metadata). Default is false for performance.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the media asset if found; otherwise, null.</returns>
        Task<MediaAsset?> GetByContentHashAsync(string contentHash, bool includeRelated = false);

        /// <summary>
        /// Increments the view count for a media asset.
        /// </summary>
        /// <param name="id">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task IncrementViewCountAsync(int id);
    }
}
