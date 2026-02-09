using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Core.Queries;
using MediaAssetManager.Services.Interfaces;

namespace MediaAssetManager.Services
{
    /// <summary>
    /// Service for playlist business logic operations
    /// </summary>
    public class PlaylistService() : IPlaylistService
    {
        /// <inheritdoc/>
        public Task<bool> AddAssetAsync(int playlistId, int assetId, int userId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Playlist> CreateAsync(Playlist playlist, int userId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PagedResult<Playlist>> GetAsync(PlaylistQuery query)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Playlist?> GetByIdAsync(int id, bool includeRelated = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> RemoveAssetAsync(int playlistId, int assetId, int userId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Playlist?> UpdateAsync(Playlist playlist, int userId)
        {
            throw new NotImplementedException();
        }
    }
}
