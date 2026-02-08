using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for User data access operations
    /// </summary>
    public class UserRepository(MediaAssetContext context) : IUserRepository
    {
        /// <inheritdoc/>
        public async Task<User> AddAsync(User user)
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Return shallow object (caller can reload with related entities if needed)
            return (await GetByIdAsync(user.UserId))!;
        }

        /// <inheritdoc/>
        public Task<User?> GetByIdAsync(int id, bool includeRelated = false)
        {
            var query = context.Users.AsNoTracking();

            // Only include related entities if explicitly requested
            if (includeRelated)
                query = IncludeRelatedEntities(query);

            return query.FirstOrDefaultAsync(u => u.UserId == id);
        }

        /// <inheritdoc/>
        public Task<User?> GetByUsernameAsync(string username, bool includeRelated = false)
        {
            var query = context.Users.AsNoTracking();

            // Only include related entities if explicitly requested
            if (includeRelated)
                query = IncludeRelatedEntities(query);

            return query.FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <inheritdoc/>
        public async Task<User?> UpdateAsync(User user)
        {
            // Check if entity exists first (avoid exception)
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (existingUser == null)
                return null;

            // Update all properties from incoming entity
            context.Entry(existingUser).CurrentValues.SetValues(user);
            await context.SaveChangesAsync();

            // Return shallow object (caller can reload with related entities if needed)
            return await GetByIdAsync(user.UserId);
        }

        // Convenience method to include related entities when requested
        private static IQueryable<User> IncludeRelatedEntities(IQueryable<User> query)
        {
            return query
                .Include(u => u.Assets)
                .Include(u => u.Playlists)
                .Include(u => u.Favorites);
        }
    }
}