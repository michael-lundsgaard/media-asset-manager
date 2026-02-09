using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for playlist data access operations.
    /// </summary>
    public interface IPlaylistRepository
    {
        /// <summary>
        /// Retrieves a playlist by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the playlist.</param>
        /// <param name="includeRelated">Whether to include related entities such as user and assets.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the playlist if found; otherwise, null.</returns>
        Task<Playlist?> GetByIdAsync(int id, bool includeRelated = false);

        /// <summary>
        /// Retrieves playlists based on the provided query criteria with filtering, sorting, and pagination.
        /// </summary>
        /// <param name="query">The query containing filter, sort, and pagination criteria.</param>
        /// <param name="includeRelated">Whether to include related entities such as user and assets.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result of playlists.</returns>
        Task<PagedResult<Playlist>> GetAsync(PlaylistQuery query, bool includeRelated = false);

        /// <summary>
        /// Adds a new playlist to the repository.
        /// </summary>
        /// <param name="playlist">The playlist to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added playlist with updated properties.</returns>
        Task<Playlist> AddAsync(Playlist playlist);

        /// <summary>
        /// Updates an existing playlist in the repository.
        /// </summary>
        /// <param name="playlist">The playlist with updated values.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated playlist if found; otherwise, null.</returns>
        Task<Playlist?> UpdateAsync(Playlist playlist);

        /// <summary>
        /// Deletes a playlist from the repository by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the playlist to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the playlist was deleted; otherwise, false.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Adds a media asset to a playlist.
        /// </summary>
        /// <param name="playlistId">The unique identifier of the playlist.</param>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the asset was added; otherwise, false.</returns>
        Task<bool> AddAssetToPlaylistAsync(int playlistId, int assetId);

        /// <summary>
        /// Removes a media asset from a playlist.
        /// </summary>
        /// <param name="playlistId">The unique identifier of the playlist.</param>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the asset was removed; otherwise, false.</returns>
        Task<bool> RemoveAssetFromPlaylistAsync(int playlistId, int assetId);
    }
}
