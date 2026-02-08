using MediaAssetManager.Core.Entities;

namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Service for OAuth client credentials authentication
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user using OAuth client credentials and generates a JWT token.
        /// </summary>
        /// <param name="clientId">The OAuth client ID.</param>
        /// <param name="clientSecret">The OAuth client secret.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authentication result with JWT token.</returns>
        Task<AuthenticationResult> AuthenticateAsync(string clientId, string clientSecret);

        /// <summary>
        /// Generates new OAuth client credentials for a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated credentials.</returns>
        Task<ClientCredentials> GenerateClientCredentialsAsync(int userId);

        /// <summary>
        /// Validates a JWT token and returns the user ID if valid.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user ID if valid; otherwise, null.</returns>
        Task<int?> ValidateTokenAsync(string token);

        /// <summary>
        /// Revokes OAuth client credentials for a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RevokeCredentialsAsync(int userId);
    }

    /// <summary>
    /// Result of authentication attempt
    /// </summary>
    public record AuthenticationResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public string? AccessToken { get; init; }
        public int ExpiresInSeconds { get; init; }
        public string? TokenType { get; init; } = "Bearer";
        public User? User { get; init; }
    }

    /// <summary>
    /// Generated OAuth client credentials
    /// </summary>
    public record ClientCredentials
    {
        public string ClientId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty; // Plain text (only shown once)
        public DateTime CreatedAt { get; init; }
    }
}
