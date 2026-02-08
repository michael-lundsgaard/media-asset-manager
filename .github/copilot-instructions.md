# GitHub Copilot Instructions - Media Asset Manager

## Project Overview

**Media Asset Manager** is a .NET 8 Web API for managing gaming clips and media assets with cloud storage integration. This is a **portfolio project** built to demonstrate clean architecture, video processing capabilities, and modern .NET development practices.

### Core Purpose

- Upload and manage gaming clips (video files)
- Extract video metadata using FFmpeg
- Generate thumbnails and compress videos
- Store files in Backblaze B2 cloud storage
- Provide analytics and user engagement features (favorites, playlists)
- OAuth client credentials authentication

### Tech Stack

- .NET 8 (LTS)
- ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL with Npgsql
- Backblaze B2 (S3-compatible storage via AWS SDK)
- FFmpeg for video processing
- JWT for authentication

---

## Architecture Guidelines

This project follows **Clean Architecture** principles with clear separation of concerns:

```
MediaAssetManager/
├── MediaAssetManager.API/            # Presentation layer (Controllers, DTOs, Middleware)
├── MediaAssetManager.Services/       # Application layer (Business logic, Video processing)
├── MediaAssetManager.Core/           # Domain layer (Entities, Interfaces, Queries)
└── MediaAssetManager.Infrastructure/ # Data access layer (Repositories, EF Core, Migrations)
```

### Dependency Rules

- **API** → Services → Core ← Infrastructure
- **Core** has NO dependencies (pure domain logic)
- **Infrastructure** depends only on Core
- **Services** depends only on Core
- **API** can depend on all layers (composition root)

### Key Principles

1. **Interfaces in Core**: All repository and service interfaces belong in `MediaAssetManager.Core.Interfaces`
2. **Implementations in Infrastructure/Services**: Concrete implementations belong in their respective layers
3. **Dependency Injection**: Use constructor injection for all dependencies
4. **DTOs for API Responses**: Never expose domain entities directly from controllers

---

## Entity Design Best Practices (EF Core Code-First)

### ✅ DO: Follow Single Responsibility Principle

Each entity should have **one clear purpose**:

```csharp
// ✅ GOOD: Lean core entity
public class MediaAsset
{
    public int AssetId { get; set; }
    public int UserId { get; set; }
    public string FileName { get; set; }
    // ... Core properties only
}

// ✅ GOOD: Separate technical metadata
public class VideoMetadata
{
    public int VideoMetadataId { get; set; }
    public int AssetId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    // ... Video-specific properties
}
```

```csharp
// ❌ BAD: Bulky entity mixing concerns
public class MediaAsset
{
    // Core + Video + Processing + Analytics + Storage ALL IN ONE
    public int Width { get; set; }
    public string? CompressedStoragePath { get; set; }
    public ProcessingStatus Status { get; set; }
    public int ViewCount { get; set; }
    // ... 40+ properties
}
```

### ✅ DO: Use Proper Relationships

```csharp
// 1:0..1 relationship (MediaAsset can exist without VideoMetadata)
modelBuilder.Entity<MediaAsset>()
    .HasOne(e => e.VideoMetadata)
    .WithOne(v => v.Asset)
    .HasForeignKey<VideoMetadata>(v => v.AssetId)
    .OnDelete(DeleteBehavior.Cascade);

// 1:N relationship
modelBuilder.Entity<User>()
    .HasMany(u => u.Assets)
    .WithOne(a => a.User)
    .HasForeignKey(a => a.UserId)
    .OnDelete(DeleteBehavior.Cascade);

// N:M relationship via join table
modelBuilder.Entity<PlaylistItem>()
    .HasOne(pi => pi.Playlist)
    .WithMany(p => p.Items)
    .HasForeignKey(pi => pi.PlaylistId);
```

### ✅ DO: Add Strategic Indexes

```csharp
// Single column indexes for frequent queries
entity.HasIndex(e => e.UserId);
entity.HasIndex(e => e.ContentHash);  // For duplicate detection
entity.HasIndex(e => e.UploadedAt);

// Composite indexes for common query combinations
entity.HasIndex(e => new { e.IsPublic, e.Status });
entity.HasIndex(e => new { e.AssetId, e.ViewedAt });  // Analytics queries

// Unique indexes for business constraints
entity.HasIndex(e => new { e.UserId, e.AssetId }).IsUnique();  // Prevent duplicate favorites
```

### ✅ DO: Use Appropriate Data Types

```csharp
// Use long for high-volume tables
public class AssetView
{
    public long ViewId { get; set; }  // NOT int (millions of records expected)
}

// Use nullable reference types
public string? Description { get; set; }  // Can be null
public string Title { get; set; } = string.Empty;  // Required

// Use enums for fixed sets
public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

// Use JSON columns for flexible data (PostgreSQL)
public List<string> Tags { get; set; } = new();

// In OnModelCreating:
entity.Property(e => e.Tags).HasColumnType("jsonb");
```

### ❌ DON'T: Use Denormalized Counters Without Strategy

```csharp
// ❌ BAD: Manual counter that can drift out of sync
public int ViewCount { get; set; } = 0;
public int FavoriteCount { get; set; } = 0;

// ✅ BETTER: Compute from relationships or use database-level triggers
public int ViewCount => Views.Count;  // Computed property

// ✅ OR: Use cached counter with clear sync strategy
public int ViewCount { get; set; } = 0;  // Cached, updated via IncrementViewCountAsync()
```

### ❌ DON'T: Mix Storage Paths with Domain Logic

```csharp
// ❌ BAD: Multiple storage paths in entity
public string OriginalStoragePath { get; set; }
public string? CompressedStoragePath { get; set; }
public string? ThumbnailStoragePath { get; set; }

// ✅ BETTER: Single primary path, optional thumbnail
public string StoragePath { get; set; }  // Primary video file
public string? ThumbnailPath { get; set; }  // Separate concern

// ✅ OR: Separate AssetFile entity for multiple file types (if needed later)
public class AssetFile
{
    public int FileId { get; set; }
    public int AssetId { get; set; }
    public FileType Type { get; set; }  // Original, Compressed, Thumbnail
    public string StoragePath { get; set; }
}
```

---

## Current Entity Model

### Core Entities

1. **MediaAsset** - Core media file entity (lean design)
2. **VideoMetadata** - Technical video specs (1:0..1 with MediaAsset)
3. **User** - User accounts with OAuth credentials
4. **Favorite** - User favorites/likes
5. **Playlist** - User-created collections
6. **PlaylistItem** - Join table for Playlist-MediaAsset relationship
7. **AssetView** - Analytics tracking (time-series data)

### Key Features Implemented

- **Content Hash (SHA256)**: Duplicate detection before upload
- **Processing Status**: Track video processing pipeline
- **OAuth Client Credentials**: API authentication
- **Lean Design**: Separated concerns, proper normalization

---

## Code Style Conventions

### Naming Conventions

```csharp
// PostgreSQL snake_case for database
// C# PascalCase for code

public class MediaAsset  // Table: media_assets
{
    public int AssetId { get; set; }  // Column: asset_id
    public string FileName { get; set; }  // Column: file_name
}
```

### Async/Await

```csharp
// ✅ Always use async suffix for async methods
public async Task<MediaAsset?> GetByIdAsync(int id)

// ✅ Always await or return Task, never both
public async Task<MediaAsset> AddAsync(MediaAsset asset)
{
    await _context.MediaAssets.AddAsync(asset);
    await _context.SaveChangesAsync();
    return asset;
}
```

### Nullable Reference Types

```csharp
// ✅ Enable nullable reference types (already enabled in project)
public string Title { get; set; } = string.Empty;  // Required
public string? Description { get; set; }  // Optional
public User User { get; set; } = null!;  // Required navigation property
public User? User { get; set; }  // Optional navigation property
```

### XML Documentation

```csharp
/// <summary>
/// Retrieves a media asset by its unique identifier.
/// </summary>
/// <param name="id">The unique identifier of the media asset.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the media asset if found; otherwise, null.</returns>
Task<MediaAsset?> GetByIdAsync(int id);
```

---

## Video Processing Guidelines

### FFmpeg Integration

- Use `FFMpegCore` NuGet package
- Store temporary files in `Path.GetTempPath()`
- Always clean up temp files in `finally` blocks
- Process videos **BEFORE** uploading to B2 (minimize egress costs)

### Processing Pipeline

1. **Upload** → Save to temp directory
2. **Extract Metadata** → FFprobe for video properties
3. **Generate Thumbnail** → FFmpeg at 3-second mark
4. **Compress** → Optional, based on file size/resolution
5. **Upload to B2** → Only the final processed files
6. **Save to Database** → Metadata + storage paths
7. **Cleanup** → Delete temp files

### Compression Strategy

```csharp
// Only compress if:
// - File size > 100MB
// - Resolution > 720p
// - User is not Premium tier

if (fileSizeBytes > 100_000_000 && height > 720 && userRole != UserRole.Premium)
{
    await _compressionService.CompressVideoAsync(inputPath, outputPath, targetHeight: 720);
}
```

---

## Authentication & Security

### OAuth Client Credentials Flow

```
Client → POST /api/auth/token (clientId, clientSecret)
       ← JWT Access Token (Bearer token)

Client → GET /api/mediaassets (Authorization: Bearer {token})
       ← Media assets response
```

### JWT Configuration

- Token expiration: 1 hour (configurable in appsettings.json)
- Store JWT secret in appsettings.json (or User Secrets for development)
- Use BCrypt for hashing client secrets
- Never log or expose client secrets after initial generation

---

## Testing Strategy

### Unit Tests (To Be Implemented)

- Services layer (business logic)
- Repository layer (data access)
- Mock dependencies using interfaces
- Target: >80% code coverage

### Integration Tests (To Be Implemented)

- API endpoints end-to-end
- Use in-memory database or test containers
- Verify request/response contracts

---

## Documentation References

### Internal Documentation

- [README.md](../README.md) - Project overview and features
- [docs/IMPLEMENTATION_GUIDE.md](../docs/IMPLEMENTATION_GUIDE.md) - V1 detailed implementation guide (reference for advanced features)
- [docs/IMPLEMENTATION_GUIDE_V2.md](../docs/IMPLEMENTATION_GUIDE_V2.md) - V2 streamlined guide (current approach)
- [docs/TODO.md](../docs/TODO.md) - Planned improvements and future features

### External Resources

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
- [FFMpegCore Library](https://github.com/rosenbjerg/FFMpegCore)
- [Backblaze B2 Documentation](https://www.backblaze.com/b2/docs/)
- [AWS SDK for .NET (S3)](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/s3-apis-intro.html)
- [JWT Best Practices](https://jwt.io/introduction)

---

## Common Tasks for Copilot

### "Implement a new repository"

1. Create interface in `MediaAssetManager.Core.Interfaces`
2. Implement in `MediaAssetManager.Infrastructure.Repositories`
3. Register in Dependency Injection (`Program.cs`)
4. Follow existing patterns (async, error handling, null checks)

### "Add a new entity"

1. Create entity in `MediaAssetManager.Core.Entities`
2. Add DbSet to `MediaAssetContext`
3. Configure relationships in `OnModelCreating`
4. Add indexes for common queries
5. Create migration: `dotnet ef migrations add <MigrationName>`

### "Create a new API endpoint"

1. Add method to controller in `MediaAssetManager.API.Controllers`
2. Create request DTO in `MediaAssetManager.API.DTOs`
3. Create response DTO (or reuse existing)
4. Add XML documentation
5. Use proper HTTP verbs and status codes
6. Never expose domain entities directly

### "Add video processing feature"

1. Create interface in `MediaAssetManager.Services.Interfaces`
2. Create service class in `MediaAssetManager.Services`
3. Use FFMpegCore for video operations
4. Handle errors and cleanup temp files
5. Return structured results (success, error message, output paths)

---

## Important Notes

- **PostgreSQL-specific**: Use `jsonb` for flexible data, not `json`
- **B2 Free Tier**: Minimize downloads from B2, process locally before upload
- **Snake_case**: All database names use snake_case (automatic conversion in DbContext)
- **Nullable Reference Types**: Enabled project-wide, use `?` and `= null!` appropriately
- **Portfolio Focus**: Prioritize working features over perfect architecture

---

## Quick Reference

### Run Migrations

```bash
dotnet ef migrations add <MigrationName> --project MediaAssetManager.Infrastructure --startup-project MediaAssetManager.API
dotnet ef database update --project MediaAssetManager.Infrastructure --startup-project MediaAssetManager.API
```

### Run Application

```bash
cd MediaAssetManager.API
dotnet run
```

### Test Endpoints

- Swagger UI: `https://localhost:7xxx/swagger`
- Health Check: `GET /api/test`

---

**When in doubt, follow existing patterns in the codebase. Consistency > Cleverness.**
