using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Enums;

namespace MediaAssetManager.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for user data access operations.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="includeRelated">Whether to include related entities (assets, playlists, favorites). Default is false for performance.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user if found; otherwise, null.</returns>
        Task<User?> GetByIdAsync(int id, bool includeRelated = false);

        /// <summary>
        /// Retrieves a user by their username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="includeRelated">Whether to include related entities (assets, playlists, favorites). Default is false for performance.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user if found; otherwise, null.</returns>
        Task<User?> GetByUsernameAsync(string username, bool includeRelated = false);

        /// <summary>
        /// Adds a new user to the repository.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added user with updated properties.</returns>
        Task<User> AddAsync(User user);

        /// <summary>
        /// Updates an existing user in the repository.
        /// </summary>
        /// <param name="user">The user with updated values.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated user if found; otherwise, null.</returns>
        Task<User?> UpdateAsync(User user);
    }
}
