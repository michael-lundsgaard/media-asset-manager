using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for playlist business logic operations.
    /// </summary>
    public interface IPlaylistService
    {
        /// <summary>
        /// Retrieves a paged list of playlists based on the specified query criteria.
        /// </summary>
        /// <param name="query">The query criteria for filtering, sorting, and pagination.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result of playlists.</returns>
        Task<PagedResult<Playlist>> GetAsync(PlaylistQuery query);

        /// <summary>
        /// Retrieves a playlist by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the playlist.</param>
        /// <param name="includeRelated">Whether to include related entities.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the playlist if found; otherwise, null.</returns>
        Task<Playlist?> GetByIdAsync(int id, bool includeRelated = false);

        /// <summary>
        /// Creates a new playlist with business validation.
        /// </summary>
        /// <param name="playlist">The playlist to create.</param>
        /// <param name="userId">The ID of the user creating the playlist.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created playlist.</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        Task<Playlist> CreateAsync(Playlist playlist, int userId);

        /// <summary>
        /// Updates an existing playlist with business validation and authorization.
        /// </summary>
        /// <param name="playlist">The playlist with updated values.</param>
        /// <param name="userId">The ID of the user attempting the update.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated playlist if successful; otherwise, null.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the playlist.</exception>
        Task<Playlist?> UpdateAsync(Playlist playlist, int userId);

        /// <summary>
        /// Deletes a playlist with authorization check.
        /// </summary>
        /// <param name="id">The unique identifier of the playlist.</param>
        /// <param name="userId">The ID of the user attempting the deletion.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if deleted; otherwise, false.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the playlist.</exception>
        Task<bool> DeleteAsync(int id, int userId);

        /// <summary>
        /// Adds a media asset to a playlist with validation.
        /// </summary>
        /// <param name="playlistId">The playlist ID.</param>
        /// <param name="assetId">The asset ID.</param>
        /// <param name="userId">The ID of the user making the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if added; otherwise, false.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the playlist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when asset doesn't exist or is already in playlist.</exception>
        Task<bool> AddAssetAsync(int playlistId, int assetId, int userId);

        /// <summary>
        /// Removes a media asset from a playlist with authorization.
        /// </summary>
        /// <param name="playlistId">The playlist ID.</param>
        /// <param name="assetId">The asset ID.</param>
        /// <param name="userId">The ID of the user making the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if removed; otherwise, false.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the playlist.</exception>
        Task<bool> RemoveAssetAsync(int playlistId, int assetId, int userId);
    }
}
