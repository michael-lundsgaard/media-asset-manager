using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for Playlist data access operations
    /// </summary>
    public class PlaylistRepository(MediaAssetContext context) : IPlaylistRepository
    {
        /// <inheritdoc/>
        public async Task<bool> AddAssetToPlaylistAsync(int playlistId, int assetId)
        {
            var playlist = await context.Playlists
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PlaylistId == playlistId);

            if (playlist == null)
                return false;

            // Check if asset already exists in playlist
            if (playlist.Items.Any(i => i.AssetId == assetId))
                return false;

            playlist.Items.Add(new PlaylistItem
            {
                AssetId = assetId,
                AddedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<Playlist> AddAsync(Playlist playlist)
        {
            playlist.CreatedAt = DateTime.UtcNow;

            await context.Playlists.AddAsync(playlist);
            await context.SaveChangesAsync();

            // Return shallow object (caller can reload with related entities if needed)
            return (await GetByIdAsync(playlist.PlaylistId))!;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var playlist = await context.Playlists.FindAsync(id);

            if (playlist == null)
                return false;


            context.Playlists.Remove(playlist);
            await context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc/>
        public async Task<Playlist?> GetByIdAsync(int id, bool includeRelated = false)
        {
            var query = context.Playlists.AsNoTracking();

            // Only include related entities if explicitly requested
            if (includeRelated)
                query = IncludeRelatedEntities(query);


            return await query.FirstOrDefaultAsync(p => p.PlaylistId == id);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Playlist>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<PagedResult<MediaAsset>> GetPlaylistAssetsAsync(int playlistId, int page = 1, int pageSize = 50)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PagedResult<Playlist>> GetPublicPlaylistsAsync(int page = 1, int pageSize = 20)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveAssetFromPlaylistAsync(int playlistId, int assetId)
        {
            var playlist = await context.Playlists
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PlaylistId == playlistId);

            if (playlist == null)
                return false;

            var item = playlist.Items.FirstOrDefault(i => i.AssetId == assetId);
            if (item == null)
                return false;

            playlist.Items.Remove(item);
            await context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc/>
        public async Task<Playlist?> UpdateAsync(Playlist playlist)
        {
            var existingPlaylist = await context.Playlists
                .FirstOrDefaultAsync(p => p.PlaylistId == playlist.PlaylistId);

            if (existingPlaylist == null)
                return null;

            // Update all properties from incoming entity
            context.Entry(existingPlaylist).CurrentValues.SetValues(playlist);
            existingPlaylist.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Return shallow object (caller can reload with related entities if needed)
            return await GetByIdAsync(playlist.PlaylistId);
        }

        // Convenience method to include related entities when requested
        private static IQueryable<Playlist> IncludeRelatedEntities(IQueryable<Playlist> query)
        {
            return query
                .Include(p => p.User)
                .Include(p => p.Items)
                    .ThenInclude(pi => pi.Asset);
        }
    }
}