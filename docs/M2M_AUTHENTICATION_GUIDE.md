# Machine-to-Machine (M2M) Authentication Implementation Guide

## Overview

This guide documents the implementation of OAuth 2.0 Client Credentials flow for machine-to-machine authentication in the Media Asset Manager API.

### Architecture Decision

**Trust Model**: The API uses a simplified M2M authentication pattern suitable for multi-client architecture:

- **Discord Bot** → Authenticates with ClientId/Secret → Calls API on behalf of users
- **Web App** → Authenticates with ClientId/Secret → Calls API on behalf of users
- **Dashboard** → Authenticates with ClientId/Secret → Calls API on behalf of users

### Key Concepts

1. **Client Authentication**: Applications authenticate to the API using client credentials
2. **User Context**: Authenticated clients provide `userId` in requests to indicate which user's resources to access
3. **Trust Boundary**: The API trusts authenticated clients to provide valid user context
4. **No User Passwords**: The API does not handle user passwords or direct user authentication

---

## Implementation Steps

### 1. Core Entities

#### Update Client Entity

**File**: `MediaAssetManager.Core/Entities/Client.cs`

Add optional fields for better tracking:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// Client entity - represents an OAuth client application that can authenticate and interact with the API.
    /// </summary>
    /// <remarks>
    /// This is separate from User to allow for multiple clients per user and better security practices (hashed secrets).
    /// </remarks>
    public class Client
    {
        public int ClientId { get; set; }
        public string ClientPublicId { get; set; } = string.Empty; // The actual clientId (GUID)
        public string ClientSecretHash { get; set; } = string.Empty; // BCrypt hashed
        public string ClientName { get; set; } = string.Empty; // "Discord Bot", "Web App"
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}
```

---

### 2. Repository Layer

#### IClientRepository Interface

**File**: `MediaAssetManager.Core/Interfaces/IClientRepository.cs`

```csharp
using MediaAssetManager.Core.Entities;

namespace MediaAssetManager.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for client data access operations.
    /// </summary>
    public interface IClientRepository
    {
        /// <summary>
        /// Retrieves a client by their unique internal identifier.
        /// </summary>
        /// <param name="id">The unique internal identifier of the client.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the client if found; otherwise, null.</returns>
        Task<Client?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves a client by their public client ID (used for authentication).
        /// </summary>
        /// <param name="clientPublicId">The public client identifier (GUID).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the client if found; otherwise, null.</returns>
        Task<Client?> GetByPublicIdAsync(string clientPublicId);

        /// <summary>
        /// Retrieves all active clients.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of active clients.</returns>
        Task<IEnumerable<Client>> GetAllActiveAsync();

        /// <summary>
        /// Adds a new client to the repository.
        /// </summary>
        /// <param name="client">The client to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added client with updated properties.</returns>
        Task<Client> AddAsync(Client client);

        /// <summary>
        /// Updates an existing client in the repository.
        /// </summary>
        /// <param name="client">The client with updated values.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated client if found; otherwise, null.</returns>
        Task<Client?> UpdateAsync(Client client);

        /// <summary>
        /// Updates the last used timestamp for a client after successful authentication.
        /// </summary>
        /// <param name="clientId">The unique internal identifier of the client.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateLastUsedAsync(int clientId);

        /// <summary>
        /// Deactivates a client (soft delete).
        /// </summary>
        /// <param name="id">The unique internal identifier of the client.</param>
        /// <returns>A task that represents the asynchronous operation. Returns true if deactivated successfully; otherwise, false.</returns>
        Task<bool> DeactivateAsync(int id);
    }
}
```

#### ClientRepository Implementation

**File**: `MediaAssetManager.Infrastructure/Repositories/ClientRepository.cs`

```csharp
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for Client data access operations
    /// </summary>
    public class ClientRepository(MediaAssetContext context) : IClientRepository
    {
        /// <inheritdoc/>
        public async Task<Client> AddAsync(Client client)
        {
            await context.Clients.AddAsync(client);
            await context.SaveChangesAsync();

            return (await GetByIdAsync(client.ClientId))!;
        }

        /// <inheritdoc/>
        public Task<Client?> GetByIdAsync(int id)
        {
            return context.Clients
                .FirstOrDefaultAsync(c => c.ClientId == id);
        }

        /// <inheritdoc/>
        public Task<Client?> GetByPublicIdAsync(string clientPublicId)
        {
            return context.Clients
                .FirstOrDefaultAsync(c => c.ClientPublicId == clientPublicId);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Client>> GetAllActiveAsync()
        {
            return await context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClientName)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Client?> UpdateAsync(Client client)
        {
            var existingClient = await context.Clients
                .FirstOrDefaultAsync(c => c.ClientId == client.ClientId);

            if (existingClient == null)
                return null;

            context.Entry(existingClient).CurrentValues.SetValues(client);
            await context.SaveChangesAsync();

            return await GetByIdAsync(client.ClientId);
        }

        /// <inheritdoc/>
        public async Task UpdateLastUsedAsync(int clientId)
        {
            var client = await context.Clients.FindAsync(clientId);

            if (client != null)
            {
                client.LastUsedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateAsync(int id)
        {
            var client = await context.Clients.FindAsync(id);

            if (client == null)
                return false;

            client.IsActive = false;
            await context.SaveChangesAsync();

            return true;
        }
    }
}
```

---

### 3. Database Configuration

#### Update MediaAssetContext

**File**: `MediaAssetManager.Infrastructure/Data/MediaAssetContext.cs`

Add the Client DbSet:

```csharp
// === DBSETS ===
public DbSet<MediaAsset> MediaAssets { get; set; } = null!;
public DbSet<User> Users { get; set; } = null!;
public DbSet<Client> Clients { get; set; } = null!;  // ADD THIS
public DbSet<VideoMetadata> VideoMetadata { get; set; } = null!;
public DbSet<Favorite> Favorites { get; set; } = null!;
public DbSet<Playlist> Playlists { get; set; } = null!;
public DbSet<PlaylistItem> PlaylistItems { get; set; } = null!;
public DbSet<AssetView> AssetViews { get; set; } = null!;
```

Add entity configuration in `OnModelCreating`:

```csharp
// === CLIENT (M2M Authentication) ===
modelBuilder.Entity<Client>(entity =>
{
    entity.HasKey(e => e.ClientId);

    entity.Property(e => e.ClientPublicId)
        .IsRequired()
        .HasMaxLength(50); // GUID string

    entity.Property(e => e.ClientSecretHash)
        .IsRequired();

    entity.Property(e => e.ClientName)
        .IsRequired()
        .HasMaxLength(100);

    entity.Property(e => e.Description)
        .HasMaxLength(500);

    // Indexes
    entity.HasIndex(e => e.ClientPublicId).IsUnique();
    entity.HasIndex(e => e.IsActive);
});
```

---

### 4. Service Layer

#### IAuthService Interface

**File**: `MediaAssetManager.Core/Interfaces/IAuthService.cs`

```csharp
using MediaAssetManager.Core.Entities;

namespace MediaAssetManager.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for authentication and authorization operations.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a client using client credentials and generates a JWT token.
        /// </summary>
        /// <param name="clientId">The public client identifier.</param>
        /// <param name="clientSecret">The client secret.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the JWT token if authentication succeeds; otherwise, null.</returns>
        Task<string?> AuthenticateClientAsync(string clientId, string clientSecret);

        /// <summary>
        /// Registers a new client and generates credentials.
        /// </summary>
        /// <param name="clientName">The friendly name for the client.</param>
        /// <param name="description">Optional description of the client's purpose.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple of (Client, PlainTextSecret).</returns>
        Task<(Client Client, string PlainTextSecret)> RegisterClientAsync(string clientName, string? description = null);

        /// <summary>
        /// Generates a JWT token for an authenticated client.
        /// </summary>
        /// <param name="client">The authenticated client.</param>
        /// <returns>A JWT token string.</returns>
        string GenerateToken(Client client);

        /// <summary>
        /// Hashes a client secret using BCrypt.
        /// </summary>
        /// <param name="secret">The plain-text secret to hash.</param>
        /// <returns>The BCrypt hashed secret.</returns>
        string HashSecret(string secret);

        /// <summary>
        /// Verifies a plain-text secret against a hashed secret.
        /// </summary>
        /// <param name="secret">The plain-text secret.</param>
        /// <param name="hashedSecret">The hashed secret to verify against.</param>
        /// <returns>True if the secret matches; otherwise, false.</returns>
        bool VerifySecret(string secret, string hashedSecret);
    }
}
```

#### JWT Configuration

**File**: `MediaAssetManager.Services/Configuration/JwtSettings.cs`

```csharp
namespace MediaAssetManager.Services.Configuration
{
    /// <summary>
    /// Configuration settings for JWT token generation
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// The secret key used to sign JWT tokens
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// The issuer of the token (usually the API URL)
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// The audience for the token (who can use it)
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time in minutes
        /// </summary>
        public int ExpirationMinutes { get; set; } = 60;
    }
}
```

#### AuthService Implementation

**File**: `MediaAssetManager.Services/AuthService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Services.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediaAssetManager.Services
{
    /// <summary>
    /// Service for handling authentication and JWT token generation
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IClientRepository _clientRepository;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IClientRepository clientRepository,
            IOptions<JwtSettings> jwtSettings)
        {
            _clientRepository = clientRepository;
            _jwtSettings = jwtSettings.Value;
        }

        /// <inheritdoc/>
        public async Task<string?> AuthenticateClientAsync(string clientId, string clientSecret)
        {
            // Retrieve client by public ID
            var client = await _clientRepository.GetByPublicIdAsync(clientId);

            // Validate client exists, is active, and secret matches
            if (client == null || !client.IsActive)
                return null;

            if (!VerifySecret(clientSecret, client.ClientSecretHash))
                return null;

            // Update last used timestamp
            await _clientRepository.UpdateLastUsedAsync(client.ClientId);

            // Generate and return JWT token
            return GenerateToken(client);
        }

        /// <inheritdoc/>
        public async Task<(Client Client, string PlainTextSecret)> RegisterClientAsync(
            string clientName,
            string? description = null)
        {
            // Generate client credentials
            var clientPublicId = Guid.NewGuid().ToString();
            var plainTextSecret = GenerateSecureSecret();
            var hashedSecret = HashSecret(plainTextSecret);

            // Create client entity
            var client = new Client
            {
                ClientPublicId = clientPublicId,
                ClientSecretHash = hashedSecret,
                ClientName = clientName,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            await _clientRepository.AddAsync(client);

            return (client, plainTextSecret);
        }

        /// <inheritdoc/>
        public string GenerateToken(Client client)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, client.ClientId.ToString()),
                    new Claim(ClaimTypes.Name, client.ClientName),
                    new Claim("client_id", client.ClientPublicId),
                    new Claim("client_name", client.ClientName)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <inheritdoc/>
        public string HashSecret(string secret)
        {
            return BCrypt.Net.BCrypt.HashPassword(secret, workFactor: 12);
        }

        /// <inheritdoc/>
        public bool VerifySecret(string secret, string hashedSecret)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(secret, hashedSecret);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random secret
        /// </summary>
        private string GenerateSecureSecret()
        {
            // Generate 32 bytes (256 bits) of random data
            var randomBytes = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            // Convert to base64 for easier handling
            return Convert.ToBase64String(randomBytes);
        }
    }
}
```

---

### 5. API Layer

#### DTOs

**File**: `MediaAssetManager.API/DTOs/Auth/TokenRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace MediaAssetManager.API.DTOs.Auth
{
    /// <summary>
    /// Request DTO for OAuth client credentials token exchange
    /// </summary>
    public class TokenRequest
    {
        /// <summary>
        /// The public client identifier
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The client secret
        /// </summary>
        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// OAuth grant type (should be "client_credentials")
        /// </summary>
        [Required]
        public string GrantType { get; set; } = "client_credentials";
    }
}
```

**File**: `MediaAssetManager.API/DTOs/Auth/TokenResponse.cs`

```csharp
namespace MediaAssetManager.API.DTOs.Auth
{
    /// <summary>
    /// Response DTO containing the access token
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// The JWT access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Token type (always "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration in seconds
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// When the token was issued
        /// </summary>
        public DateTime IssuedAt { get; set; }
    }
}
```

**File**: `MediaAssetManager.API/DTOs/Auth/ClientRegistrationRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace MediaAssetManager.API.DTOs.Auth
{
    /// <summary>
    /// Request DTO for registering a new client application
    /// </summary>
    public class ClientRegistrationRequest
    {
        /// <summary>
        /// Friendly name for the client (e.g., "Discord Bot", "Web App")
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the client's purpose
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }
    }
}
```

**File**: `MediaAssetManager.API/DTOs/Auth/ClientRegistrationResponse.cs`

```csharp
namespace MediaAssetManager.API.DTOs.Auth
{
    /// <summary>
    /// Response DTO after successful client registration
    /// WARNING: The ClientSecret is only shown once during registration!
    /// </summary>
    public class ClientRegistrationResponse
    {
        /// <summary>
        /// The public client identifier (use this for authentication)
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The client secret (ONLY shown once - store securely!)
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// The friendly name of the client
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// When this client was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Warning message about the secret
        /// </summary>
        public string Warning { get; set; } = "IMPORTANT: Store the ClientSecret securely. It will not be shown again!";
    }
}
```

#### AuthController

**File**: `MediaAssetManager.API/Controllers/AuthController.cs`

```csharp
using MediaAssetManager.API.DTOs.Auth;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MediaAssetManager.API.Controllers
{
    /// <summary>
    /// Controller for authentication and client management
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// OAuth 2.0 client credentials token endpoint
        /// </summary>
        /// <param name="request">Client credentials</param>
        /// <returns>JWT access token</returns>
        /// <response code="200">Returns the access token</response>
        /// <response code="400">Invalid request format</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Token([FromBody] TokenRequest request)
        {
            // Validate grant type
            if (request.GrantType != "client_credentials")
            {
                return BadRequest(new
                {
                    error = "unsupported_grant_type",
                    error_description = "Only 'client_credentials' grant type is supported"
                });
            }

            // Authenticate client
            var token = await _authService.AuthenticateClientAsync(
                request.ClientId,
                request.ClientSecret);

            if (token == null)
            {
                _logger.LogWarning("Failed authentication attempt for client: {ClientId}", request.ClientId);
                return Unauthorized(new
                {
                    error = "invalid_client",
                    error_description = "Invalid client credentials or inactive client"
                });
            }

            _logger.LogInformation("Client authenticated successfully: {ClientId}", request.ClientId);

            return Ok(new TokenResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = _jwtSettings.ExpirationMinutes * 60,
                IssuedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Register a new client application
        /// </summary>
        /// <param name="request">Client registration details</param>
        /// <returns>Client credentials (secret only shown once!)</returns>
        /// <response code="201">Client registered successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost("register")]
        [AllowAnonymous] // TODO: Add admin authorization once user roles are implemented
        [ProducesResponseType(typeof(ClientRegistrationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] ClientRegistrationRequest request)
        {
            var (client, plainTextSecret) = await _authService.RegisterClientAsync(
                request.ClientName,
                request.Description);

            _logger.LogInformation("New client registered: {ClientName} (ID: {ClientId})",
                client.ClientName, client.ClientPublicId);

            var response = new ClientRegistrationResponse
            {
                ClientId = client.ClientPublicId,
                ClientSecret = plainTextSecret,
                ClientName = client.ClientName,
                CreatedAt = client.CreatedAt
            };

            return CreatedAtAction(
                nameof(Register),
                new { id = client.ClientId },
                response);
        }
    }
}
```

---

### 6. Configuration

#### Install NuGet Packages

```powershell
# In MediaAssetManager.Services
dotnet add package BCrypt.Net-Next
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
```

#### Update appsettings.json

**File**: `MediaAssetManager.API/appsettings.json`

Add JWT configuration section:

```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.AspNetCore": "Warning"
		}
	},
	"AllowedHosts": "*",
	"ConnectionStrings": {
		"DefaultConnection": "your-connection-string"
	},
	"Jwt": {
		"SecretKey": "your-super-secret-key-minimum-32-characters-long",
		"Issuer": "https://localhost:7xxx",
		"Audience": "media-asset-manager-clients",
		"ExpirationMinutes": 60
	},
	"B2": {
		"KeyId": "your-b2-key",
		"KeySecret": "your-b2-secret",
		"BucketName": "your-bucket",
		"Endpoint": "https://s3.eu-central-003.backblazeb2.com"
	}
}
```

**⚠️ SECURITY NOTE**: For production, use **User Secrets** or **Environment Variables** for the JWT SecretKey!

```powershell
# Using User Secrets (Development)
dotnet user-secrets set "Jwt:SecretKey" "your-actual-secret-key-here"
```

#### Update Dependency Injection

**File**: `MediaAssetManager.API/Configuration/ServiceCollectionExtensions.cs`

Update the methods:

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IMediaAssetService, MediaAssetService>();
    services.AddScoped<IStorageService, B2StorageService>();
    services.AddScoped<IAuthService, AuthService>(); // ADD THIS

    return services;
}

public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IClientRepository, ClientRepository>(); // ADD THIS

    return services;
}
```

**File**: `MediaAssetManager.API/Program.cs`

Ensure JWT configuration is loaded:

```csharp
// === OPTIONS PATTERN (Strongly-typed configuration) ===
builder.Services.Configure<B2StorageOptions>(
    builder.Configuration.GetSection(B2StorageOptions.SectionName));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt")); // ADD THIS
```

**Note**: You already have `JwtOptions` - you can reuse that instead of creating `JwtSettings`. Just ensure the property names match (rename `Secret` to `SecretKey` in JwtOptions if needed).

---

### 7. Database Migration

Create and apply the migration:

```powershell
# Create migration
dotnet ef migrations add AddClientAuthentication --project MediaAssetManager.Infrastructure --startup-project MediaAssetManager.API

# Apply migration
dotnet ef database update --project MediaAssetManager.Infrastructure --startup-project MediaAssetManager.API
```

---

## Usage Examples

### 1. Register a New Client

**Request**:

```http
POST /api/auth/register
Content-Type: application/json

{
  "clientName": "Discord Bot",
  "description": "Bot for Discord community integration"
}
```

**Response**:

```json
{
	"clientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
	"clientSecret": "lKj8H9gF6dS5aW4eR3tY2uI1oP0mN9bV8cX7zL6kJ5h4G3f2D1s0Q",
	"clientName": "Discord Bot",
	"createdAt": "2026-02-08T10:30:00Z",
	"warning": "IMPORTANT: Store the ClientSecret securely. It will not be shown again!"
}
```

**⚠️ CRITICAL**: Save the `clientSecret` immediately! It will never be shown again.

---

### 2. Authenticate and Get Access Token

**Request**:

```http
POST /api/auth/token
Content-Type: application/json

{
  "clientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "clientSecret": "lKj8H9gF6dS5aW4eR3tY2uI1oP0mN9bV8cX7zL6kJ5h4G3f2D1s0Q",
  "grantType": "client_credentials"
}
```

**Response**:

```json
{
	"accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
	"tokenType": "Bearer",
	"expiresIn": 3600,
	"issuedAt": "2026-02-08T10:35:00Z"
}
```

---

### 3. Use Token to Access Protected Endpoints

**Request**:

```http
GET /api/mediaassets?userId=123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response**:

```json
{
	"items": [
		{
			"assetId": 1,
			"title": "Epic Gaming Moment",
			"fileName": "clip_001.mp4",
			"uploadedAt": "2026-02-01T15:20:00Z"
		}
	],
	"totalCount": 1,
	"pageNumber": 1,
	"pageSize": 10
}
```

---

## Client Implementation Examples

### Discord Bot (Node.js)

```javascript
const axios = require('axios');

const API_URL = 'https://localhost:7xxx';
const CLIENT_ID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
const CLIENT_SECRET = 'lKj8H9gF6dS5aW4eR3tY2uI1oP0mN9bV8cX7zL6kJ5h4G3f2D1s0Q';

let accessToken = null;

// Authenticate
async function authenticate() {
	const response = await axios.post(`${API_URL}/api/auth/token`, {
		clientId: CLIENT_ID,
		clientSecret: CLIENT_SECRET,
		grantType: 'client_credentials',
	});

	accessToken = response.data.accessToken;
	console.log('Authenticated successfully!');
}

// Get user's clips
async function getUserClips(userId) {
	const response = await axios.get(`${API_URL}/api/mediaassets`, {
		headers: {
			Authorization: `Bearer ${accessToken}`,
		},
		params: { userId },
	});

	return response.data;
}

// Usage in Discord command
client.on('messageCreate', async (message) => {
	if (message.content === '!myclips') {
		const userId = getUserIdFromDiscordId(message.author.id);
		const clips = await getUserClips(userId);
		message.reply(`You have ${clips.totalCount} clips!`);
	}
});
```

### React Web App (TypeScript)

```typescript
import axios from 'axios';

const API_URL = 'https://localhost:7xxx';
const CLIENT_ID = 'b2c3d4e5-f6g7-8901-bcde-fg2345678901';
const CLIENT_SECRET = 'mNj9I0hG7eT6bX5fS4uZ3vJ2kQ1pO0nM9cY8aM7lK6j5I4h3G2f1E0r';

class ApiClient {
  private accessToken: string | null = null;

  async authenticate(): Promise<void> {
    const response = await axios.post(`${API_URL}/api/auth/token`, {
      clientId: CLIENT_ID,
      clientSecret: CLIENT_SECRET,
      grantType: 'client_credentials'
    });

    this.accessToken = response.data.accessToken;
  }

  async getUserAssets(userId: number) {
    if (!this.accessToken) {
      await this.authenticate();
    }

    const response = await axios.get(`${API_URL}/api/mediaassets`, {
      headers: {
        'Authorization': `Bearer ${this.accessToken}`
      },
      params: { userId }
    });

    return response.data;
  }
}

// Usage in React component
const MyClipsPage = () => {
  const [clips, setClips] = useState([]);
  const apiClient = new ApiClient();

  useEffect(() => {
    const currentUserId = getCurrentUserId(); // From your auth context
    apiClient.getUserAssets(currentUserId)
      .then(data => setClips(data.items));
  }, []);

  return <div>{/* Render clips */}</div>;
};
```

---

## Security Considerations

### 1. **Client Secret Storage**

- **NEVER** commit client secrets to version control
- Store in environment variables or secure vaults
- Rotate secrets periodically

### 2. **JWT Secret Key**

- Minimum 32 characters
- Use cryptographically secure random generation
- Use User Secrets or Key Vault in production

```powershell
# Generate secure key (PowerShell)
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

### 3. **HTTPS Only**

- Always use HTTPS in production
- Never send credentials over HTTP

### 4. **Rate Limiting**

Consider adding rate limiting to prevent brute-force attacks:

```csharp
// TODO: Add rate limiting middleware
// Install: AspNetCoreRateLimit
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
services.AddInMemoryRateLimiting();
```

### 5. **Client Deactivation**

Provide admin endpoints to deactivate compromised clients:

```csharp
[HttpDelete("{clientId}/deactivate")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeactivateClient(int clientId)
{
    var result = await _clientRepository.DeactivateAsync(clientId);
    return result ? NoContent() : NotFound();
}
```

---

## Testing

### Manual Testing with Swagger

1. Start the API: `dotnet run --project MediaAssetManager.API`
2. Open Swagger UI: `https://localhost:7xxx/swagger`
3. Test `/api/auth/register` endpoint
4. Save the returned `clientSecret`
5. Test `/api/auth/token` endpoint
6. Copy the `accessToken`
7. Click "Authorize" in Swagger and paste token
8. Test protected endpoints

### Testing with cURL

```bash
# Register client
curl -X POST https://localhost:7xxx/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"clientName":"Test Client","description":"Testing"}'

# Get token
curl -X POST https://localhost:7xxx/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "clientId":"YOUR_CLIENT_ID",
    "clientSecret":"YOUR_CLIENT_SECRET",
    "grantType":"client_credentials"
  }'

# Use token
curl -X GET https://localhost:7xxx/api/mediaassets?userId=1 \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## Next Steps

### 1. **Add JWT Authentication Middleware**

Currently, the API issues tokens but doesn't validate them. Add authentication middleware:

```csharp
// In Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// After var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

### 2. **Protect Endpoints**

Add `[Authorize]` attribute to controllers:

```csharp
[ApiController]
[Route("api/mediaassets")]
[Authorize] // Require authentication
public class MediaAssetsController : ControllerBase
{
    // ...
}
```

### 3. **Extract UserId from Requests**

Modify controllers to accept userId parameter:

```csharp
[HttpGet]
public async Task<IActionResult> GetAssets([FromQuery] int userId, [FromQuery] MediaAssetQueryRequest queryRequest)
{
    // Optionally validate that authenticated client should have access to this userId
    var assets = await _mediaAssetService.GetAssetsAsync(userId, query);
    return Ok(assets);
}
```

### 4. **Admin Endpoints**

Add admin-only endpoints for client management:

```csharp
[HttpGet("clients")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ListClients()
{
    var clients = await _clientRepository.GetAllActiveAsync();
    return Ok(clients);
}
```

---

## Troubleshooting

### Token Not Accepted

**Problem**: 401 Unauthorized even with valid token

**Solutions**:

- Verify JWT middleware is configured correctly
- Check Issuer and Audience match in appsettings.json
- Ensure Bearer token is properly formatted: `Bearer {token}`
- Check token hasn't expired

### "Invalid client_credentials"

**Problem**: Cannot get token

**Solutions**:

- Verify ClientId exists in database
- Check ClientSecret matches (case-sensitive)
- Ensure client `IsActive` is true

### Database Connection Issues

**Problem**: Cannot connect to PostgreSQL

**Solutions**:

- Verify connection string in appsettings.json
- Ensure PostgreSQL service is running
- Check database exists
- Run migrations: `dotnet ef database update`

---

## Summary

This M2M authentication system provides:

✅ **Secure client authentication** with BCrypt-hashed secrets  
✅ **JWT token generation** for stateless authentication  
✅ **Multi-client support** for Discord bots, web apps, dashboards  
✅ **Trust model** where clients provide user context  
✅ **Simple integration** with standard OAuth 2.0 client credentials flow  
✅ **Portfolio-ready** demonstrating industry-standard patterns

You now have a clean, professional authentication system that separates concerns and allows multiple clients to consume your API on behalf of users.
