using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;

namespace MediaAssetManager.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for favorite data access operations.
    /// </summary>
    public interface IFavoriteRepository
    {
        /// <summary>
        /// Adds a favorite for a user and media asset.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the favorite was added; false if it already exists.</returns>
        Task<bool> AddFavoriteAsync(int userId, int assetId);

        /// <summary>
        /// Removes a favorite for a user and media asset.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the favorite was removed; otherwise, false.</returns>
        Task<bool> RemoveFavoriteAsync(int userId, int assetId);

        /// <summary>
        /// Checks if a user has favorited a media asset.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the favorite exists; otherwise, false.</returns>
        Task<bool> IsFavoritedAsync(int userId, int assetId);

        /// <summary>
        /// Retrieves all favorites for a specific user with pagination.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result of media assets.</returns>
        Task<PagedResult<MediaAsset>> GetUserFavoritesAsync(int userId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Gets the count of favorites for a specific media asset.
        /// </summary>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the favorite count.</returns>
        Task<int> GetFavoriteCountAsync(int assetId);
    }
}
