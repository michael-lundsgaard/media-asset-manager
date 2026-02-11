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
        /// <param name="expand">Optional set of navigation properties to eagerly load (e.g., "user", "videoMetadata").</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the media asset if found; otherwise, null.</returns>
        Task<MediaAsset?> GetByIdAsync(int id, HashSet<string>? expand = null);

        /// <summary>
        /// Retrieves a paged list of media assets based on the specified query criteria.
        /// </summary>
        /// <param name="query">The query criteria for filtering, sorting, and pagination. Includes Expand property for conditional navigation property loading.</param>
        Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query);

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
        /// <param name="expand">Optional set of navigation properties to eagerly load (e.g., "user", "videoMetadata").</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the media asset if found; otherwise, null.</returns>
        Task<MediaAsset?> GetByContentHashAsync(string contentHash, HashSet<string>? expand = null);
    }
}
