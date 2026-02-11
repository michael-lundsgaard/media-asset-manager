

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
        /// <param name="expand">Optional set of navigation properties to eagerly load.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the media asset if found; otherwise, null.</returns>
        Task<MediaAsset?> GetByIdAsync(int id, HashSet<string>? expand = null);

        /// <summary>
        /// Creates a new media asset with business validation.
        /// </summary>
        /// <param name="asset">The media asset to create.</param>
        /// <param name="userId">The ID of the user creating the asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created asset.</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        Task<MediaAsset> CreateAsync(MediaAsset asset, int userId);

        /// <summary>
        /// Updates an existing media asset with business validation and authorization.
        /// </summary>
        /// <param name="asset">The asset with updated values.</param>
        /// <param name="userId">The ID of the user attempting the update.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated asset if successful; otherwise, null.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the asset.</exception>
        Task<MediaAsset?> UpdateAsync(MediaAsset asset, int userId);

        /// <summary>
        /// Deletes a media asset with authorization check.
        /// </summary>
        /// <param name="id">The unique identifier of the asset.</param>
        /// <param name="userId">The ID of the user attempting the deletion.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if deleted; otherwise, false.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the asset.</exception>
        Task<bool> DeleteAsync(int id, int userId);
    }
}
