# Media Asset Manager - Next Steps & Optimization Guide

**Version:** February 2026  
**Status:** Post Circular Reference Fix & Expand System Implementation

---

## Table of Contents

1. [Immediate Next Steps](#immediate-next-steps)
2. [Performance Optimizations](#performance-optimizations)
3. [Swagger/OpenAPI Documentation](#swaggeropenapi-documentation)
4. [Architecture Improvements](#architecture-improvements)
5. [Testing Strategy](#testing-strategy)
6. [Authentication & Security](#authentication--security)
7. [Video Processing Implementation](#video-processing-implementation)
8. [Deployment Preparation](#deployment-preparation)

---

## Immediate Next Steps

### 1. Fix ViewCount Performance Issue ⚡ **HIGH PRIORITY**

**Current Problem:**

```csharp
// In MediaAssetRepository.cs - Line ~137
query = query.Include(a => a.Views);  // Loads ALL Views records into memory

// In MediaAssetMappingExtensions.cs
ViewCount = entity.Views?.Count ?? 0  // Counts in-memory (wasteful)
```

This loads **entire Views collection** (potentially thousands of records per asset) just to count them.

**Solution A: Computed Column in Database (BEST)**

1. Add computed column to `MediaAsset` entity:

```csharp
// MediaAssetManager.Core/Entities/MediaAsset.cs
public class MediaAsset
{
    // ... existing properties ...

    /// <summary>
    /// Cached view count. Updated via database trigger or batch job.
    /// Use this instead of counting Views collection.
    /// </summary>
    public int ViewCount { get; set; } = 0;
}
```

2. Update `MediaAssetContext` configuration:

```csharp
// MediaAssetManager.Infrastructure/Data/Configurations/MediaAssetConfiguration.cs
public void Configure(EntityTypeBuilder<MediaAsset> entity)
{
    // ... existing configuration ...

    // ViewCount is managed by database trigger
    entity.Property(e => e.ViewCount)
        .HasDefaultValue(0);
}
```

3. Create PostgreSQL trigger to auto-update ViewCount:

```sql
-- Migration: Add trigger to maintain ViewCount
CREATE OR REPLACE FUNCTION update_asset_view_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE media_assets
        SET view_count = view_count + 1
        WHERE asset_id = NEW.asset_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE media_assets
        SET view_count = view_count - 1
        WHERE asset_id = OLD.asset_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_view_count
AFTER INSERT OR DELETE ON asset_views
FOR EACH ROW EXECUTE FUNCTION update_asset_view_count();
```

4. Remove `Include(a => a.Views)` from repository:

```csharp
// MediaAssetRepository.cs - ApplyExpands method
private static IQueryable<MediaAsset> ApplyExpands(IQueryable<MediaAsset> query, HashSet<string>? expand)
{
    // REMOVE THIS LINE:
    // query = query.Include(a => a.Views);

    if (expand == null)
        return query;

    if (expand.Contains("user", StringComparer.OrdinalIgnoreCase))
        query = query.Include(a => a.User);

    if (expand.Contains("videoMetadata", StringComparer.OrdinalIgnoreCase))
        query = query.Include(a => a.VideoMetadata);

    return query;
}
```

5. Update mapping to use cached ViewCount:

```csharp
// MediaAssetMappingExtensions.cs
ViewCount = entity.ViewCount  // Use cached value, not collection count
```

**Benefits:**

- ✅ **100x faster** - No collection loading
- ✅ Database handles counting (optimized)
- ✅ Trigger ensures consistency
- ✅ Works with millions of views

**Solution B: Use Projection with Select() (ALTERNATIVE)**

If you don't want database triggers, use projection:

```csharp
// MediaAssetRepository.cs
public async Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query)
{
    var queryable = context.MediaAssets.AsNoTracking()
        .Select(a => new
        {
            Asset = a,
            ViewCount = a.Views.Count  // SQL COUNT(*), not in-memory
        });

    queryable = queryable.ApplyFilters(query);

    var totalCount = await queryable.CountAsync();

    var results = await queryable
        .ApplySorting(query)
        .ApplyPaging(query)
        .ToListAsync();

    // Set ViewCount on each entity
    var items = results.Select(r =>
    {
        r.Asset.ViewCount = r.ViewCount;
        return r.Asset;
    }).ToList();

    return new PagedResult<MediaAsset>(items, totalCount, query.PageNumber, query.PageSize);
}
```

**Benefits:**

- ✅ SQL-level counting (fast)
- ✅ No trigger needed
- ❌ More complex code

---

### 2. Implement Select() Projections for Optimal Performance

**Current Approach (Include):**

```csharp
// Loads entire entity + navigation properties
var assets = await context.MediaAssets
    .Include(a => a.User)
    .Include(a => a.VideoMetadata)
    .ToListAsync();
```

**Problems:**

- Loads all columns even if not needed
- Cartesian explosion with multiple Includes
- Tracking overhead (even with AsNoTracking)

**Optimized Approach (Select Projection):**

```csharp
// MediaAssetRepository.cs - NEW METHOD
public async Task<PagedResult<MediaAssetDto>> GetProjectedAsync(MediaAssetQuery query)
{
    var queryable = context.MediaAssets
        .AsNoTracking()
        .ApplyFilters(query);

    var totalCount = await queryable.CountAsync();

    // Project directly to DTO - only fetch needed columns
    var items = await queryable
        .Select(a => new MediaAssetDto
        {
            AssetId = a.AssetId,
            FileName = a.FileName,
            OriginalFileName = a.OriginalFileName,
            FileSizeBytes = a.FileSizeBytes,
            Title = a.Title,
            UploadedAt = a.UploadedAt,
            ViewCount = a.ViewCount,  // Cached value

            // Conditional projections based on query.Expand
            User = query.Expand != null && query.Expand.Contains("user")
                ? new UserSummaryDto
                {
                    UserId = a.User.UserId,
                    Username = a.User.Username
                }
                : null,

            VideoMetadata = query.Expand != null && query.Expand.Contains("videoMetadata") && a.VideoMetadata != null
                ? new VideoMetadataDto
                {
                    VideoMetadataId = a.VideoMetadata.VideoMetadataId,
                    DurationSeconds = a.VideoMetadata.DurationSeconds,
                    Width = a.VideoMetadata.Width,
                    Height = a.VideoMetadata.Height,
                    FrameRate = a.VideoMetadata.FrameRate,
                    Codec = a.VideoMetadata.Codec,
                    BitrateKbps = a.VideoMetadata.BitrateKbps,
                    AudioCodec = a.VideoMetadata.AudioCodec
                }
                : null
        })
        .ApplySorting(query)
        .ApplyPaging(query)
        .ToListAsync();

    return new PagedResult<MediaAssetDto>(items, totalCount, query.PageNumber, query.PageSize);
}
```

**Benefits:**

- ✅ **50-90% faster queries** - Only fetches needed columns
- ✅ No cartesian product issues
- ✅ Single SQL query (no N+1)
- ✅ Smaller memory footprint
- ✅ Compiled query plan caching

**Performance Comparison:**

| Approach            | Query Count    | Columns Fetched | Rows Loaded | Speed            |
| ------------------- | -------------- | --------------- | ----------- | ---------------- |
| Include()           | 1 (with JOINs) | ALL             | ALL related | Baseline         |
| Select() to Entity  | 1              | ALL             | Only needed | 2x faster        |
| **Select() to DTO** | 1              | Only needed     | Only needed | **5-10x faster** |

**Migration Path:**

1. Create DTOs that match your response models
2. Add projection methods alongside existing Include methods
3. Update controllers to use projection methods
4. Measure performance improvement
5. Remove old Include-based methods once stable

---

### 3. Implement AsSplitQuery() for Multiple Collections

**Problem: Cartesian Explosion**

When including multiple collections:

```csharp
var asset = await context.MediaAssets
    .Include(a => a.PlaylistItems)  // 10 playlists
    .Include(a => a.Favorites)      // 50 favorites
    .Include(a => a.Views)          // 1000 views
    .FirstAsync();

// SQL returns: 10 × 50 × 1000 = 500,000 rows for ONE asset!
```

**Solution: AsSplitQuery()**

```csharp
var asset = await context.MediaAssets
    .Include(a => a.PlaylistItems)
    .Include(a => a.Favorites)
    .Include(a => a.Views)
    .AsSplitQuery()  // Separate SQL queries per collection
    .FirstAsync();

// SQL executes 4 queries:
// 1. SELECT * FROM media_assets WHERE asset_id = 1
// 2. SELECT * FROM playlist_items WHERE asset_id = 1  (10 rows)
// 3. SELECT * FROM favorites WHERE asset_id = 1       (50 rows)
// 4. SELECT * FROM asset_views WHERE asset_id = 1     (1000 rows)
// Total: 1060 rows vs 500,000 rows
```

**When to Use:**

- ✅ Including 2+ collections
- ✅ Collections have many items
- ❌ Single Include (overhead not worth it)
- ❌ Using Select() projection (already optimal)

**Implementation:**

```csharp
// MediaAssetRepository.cs
modelBuilder.Entity<MediaAsset>()
    .Navigation(e => e.PlaylistItems)
    .AutoInclude(false);  // Prevent accidental includes

// Use AsSplitQuery when needed
public async Task<MediaAsset?> GetByIdWithRelatedAsync(int id)
{
    return await context.MediaAssets
        .Include(a => a.PlaylistItems)
        .Include(a => a.Favorites)
        .AsSplitQuery()
        .FirstOrDefaultAsync(a => a.AssetId == id);
}
```

---

## Performance Optimizations

### Database Indexes Strategy

**Current Indexes (Review):**

```sql
-- Verify these exist in your database
SELECT tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;
```

**Recommended Indexes:**

```sql
-- MediaAssets table
CREATE INDEX idx_media_assets_user_id ON media_assets(user_id);
CREATE INDEX idx_media_assets_uploaded_at ON media_assets(uploaded_at DESC);
CREATE INDEX idx_media_assets_content_hash ON media_assets(content_hash); -- Duplicate detection
CREATE INDEX idx_media_assets_status ON media_assets(status);

-- Composite indexes for common queries
CREATE INDEX idx_media_assets_user_status ON media_assets(user_id, status);
CREATE INDEX idx_media_assets_public_status ON media_assets(is_public, status) WHERE is_public = true;

-- AssetViews table (high write volume)
CREATE INDEX idx_asset_views_asset_id ON asset_views(asset_id);
CREATE INDEX idx_asset_views_viewed_at ON asset_views(viewed_at DESC);

-- Favorites table
CREATE INDEX idx_favorites_user_asset ON favorites(user_id, asset_id);
CREATE INDEX idx_favorites_asset_id ON favorites(asset_id);

-- Playlist items
CREATE INDEX idx_playlist_items_playlist_id ON playlist_items(playlist_id);
CREATE INDEX idx_playlist_items_asset_id ON playlist_items(asset_id);
```

**Analyze Query Performance:**

```sql
-- Find slow queries
EXPLAIN ANALYZE
SELECT a.asset_id, a.title, u.username, COUNT(v.view_id) as view_count
FROM media_assets a
LEFT JOIN users u ON a.user_id = u.user_id
LEFT JOIN asset_views v ON a.asset_id = v.asset_id
WHERE a.is_public = true
GROUP BY a.asset_id, a.title, u.username
ORDER BY a.uploaded_at DESC
LIMIT 20;
```

---

### Caching Strategy

**Level 1: Memory Cache (Response Cache)**

```csharp
// Program.cs
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// Use in middleware
app.UseResponseCaching();
```

```csharp
// MediaAssetsController.cs
[HttpGet]
[ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "page", "pageSize" })]
public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> Get(
    [FromQuery] MediaAssetQueryRequest query)
{
    // Cached for 60 seconds per unique query
    var result = await service.GetAsync(query.ToQuery());
    return Ok(result.ToPaginatedResponse());
}
```

**Level 2: Distributed Cache (Redis)**

For production with multiple servers:

```csharp
// NuGet: Microsoft.Extensions.Caching.StackExchangeRedis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MediaAssetManager_";
});
```

```csharp
// MediaAssetService.cs
public class MediaAssetService : IMediaAssetService
{
    private readonly IDistributedCache _cache;
    private readonly IMediaAssetRepository _repository;

    public async Task<MediaAsset?> GetByIdAsync(int id, HashSet<string>? expand = null)
    {
        var cacheKey = $"asset:{id}:{string.Join(",", expand ?? new HashSet<string>())}";

        // Try cache first
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<MediaAsset>(cached);

        // Fetch from database
        var asset = await _repository.GetByIdAsync(id, expand);

        if (asset != null)
        {
            // Cache for 5 minutes
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(asset),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        }

        return asset;
    }
}
```

**Caching Guidelines:**

- ✅ Cache GET requests (read-heavy)
- ✅ Short TTL for frequently updated data (1-5 min)
- ✅ Longer TTL for static data (10-60 min)
- ❌ Don't cache POST/PUT/DELETE
- ❌ Don't cache user-specific data in shared cache
- ✅ Invalidate cache on updates

---

### Pagination Improvements

**Current Implementation:**

```csharp
public PagedResult<MediaAsset>(List<MediaAsset> items, int totalCount, int pageNumber, int pageSize)
```

**Add Cursor-Based Pagination for Large Datasets:**

Cursor pagination is **much faster** for large offsets:

- Traditional: `OFFSET 1000000 LIMIT 20` - Database scans 1M rows ❌
- Cursor: `WHERE uploaded_at < '2024-01-01' LIMIT 20` - Database uses index ✅

```csharp
// MediaAssetManager.Core/Queries/MediaAssetQuery.cs
public class MediaAssetQuery
{
    // Existing pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // NEW: Cursor-based pagination
    public DateTime? CursorAfter { get; set; }  // For "next page"
    public DateTime? CursorBefore { get; set; } // For "previous page"
    public bool UseCursorPagination { get; set; } = false;
}
```

```csharp
// MediaAssetRepository.cs
if (query.UseCursorPagination && query.CursorAfter.HasValue)
{
    queryable = queryable.Where(a => a.UploadedAt < query.CursorAfter.Value);
}

// Return cursor info in response
var items = await queryable.Take(query.PageSize + 1).ToListAsync();
var hasNextPage = items.Count > query.PageSize;
if (hasNextPage) items.RemoveAt(items.Count - 1);

return new CursorPagedResult<MediaAsset>
{
    Items = items,
    HasNextPage = hasNextPage,
    NextCursor = hasNextPage ? items.Last().UploadedAt : null
};
```

---

## Swagger/OpenAPI Documentation

### 1. Add XML Documentation Generation

**Enable in Project Files:**

```xml
<!-- MediaAssetManager.API/MediaAssetManager.API.csproj -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML doc warnings -->
</PropertyGroup>
```

**Configure Swagger to Use XML Comments:**

```csharp
// Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Media Asset Manager API",
        Description = "A .NET Web API for managing gaming clips and media assets with cloud storage integration",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Url = new Uri("https://github.com/yourusername/media-asset-manager")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // Support for expand parameters
    options.OperationFilter<ExpandParameterOperationFilter>();
});
```

---

### 2. Create Custom Operation Filter for Expand

```csharp
// MediaAssetManager.API/Swagger/ExpandParameterOperationFilter.cs
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MediaAssetManager.API.Swagger
{
    /// <summary>
    /// Adds detailed documentation for expand query parameters in Swagger UI.
    /// </summary>
    public class ExpandParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null) return;

            // Find expand parameters
            var expandParam = operation.Parameters
                .FirstOrDefault(p => p.Name.Equals("expand", StringComparison.OrdinalIgnoreCase));

            if (expandParam != null)
            {
                expandParam.Description = @"
**Comma-separated list of navigation properties to include in the response.**

Supported values:
- `user` - Include user who uploaded the asset
- `videoMetadata` - Include technical video metadata (duration, resolution, codec)

**Examples:**
- `?expand=user` - Include only user info
- `?expand=videoMetadata` - Include only video metadata
- `?expand=user,videoMetadata` - Include both

**Performance Impact:**
Each expanded property requires an additional database JOIN. Only request what you need.";

                expandParam.Schema = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "string",
                        Enum = new List<IOpenApiAny>
                        {
                            new OpenApiString("user"),
                            new OpenApiString("videoMetadata")
                        }
                    }
                };

                expandParam.Example = new OpenApiString("user,videoMetadata");
            }
        }
    }
}
```

---

### 3. Add Request/Response Examples

```csharp
// MediaAssetsController.cs
[HttpGet]
[ProducesResponseType(typeof(PaginatedResponse<MediaAssetResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[SwaggerOperation(
    Summary = "Get paginated list of media assets",
    Description = @"
Retrieves a paginated list of media assets with optional filtering, sorting, and navigation property expansion.

**Filtering:**
- fileName, title, minFileSizeBytes, maxFileSizeBytes, uploadedAfter, uploadedBefore

**Sorting:**
- sortBy: UploadedAt (default), FileName, Title, FileSizeBytes
- sortDescending: true (default) or false

**Pagination:**
- page: Page number (default: 1)
- pageSize: Items per page (default: 20, max: 1000)

**Expansion:**
- expand: Comma-separated list (user, videoMetadata)

**Performance Tips:**
- Use cursor pagination for large offsets (page > 100)
- Only expand properties you need
- Cache results on client side",
    OperationId = "GetMediaAssets",
    Tags = new[] { "MediaAssets" }
)]
[SwaggerResponse(200, "Returns paginated media assets", typeof(PaginatedResponse<MediaAssetResponse>))]
[SwaggerResponse(400, "Invalid query parameters", typeof(ErrorResponse))]
public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> Get(
    [FromQuery] MediaAssetQueryRequest query)
{
    var coreQuery = query.ToQuery();
    var pagedResult = await service.GetAsync(coreQuery);
    var response = pagedResult.ToPaginatedResponse(coreQuery.Expand);
    return Ok(response);
}
```

---

### 4. Add Example Values with SwaggerSchemaFilter

```csharp
// MediaAssetManager.API/Swagger/ExampleSchemaFilter.cs
public class MediaAssetResponseSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(MediaAssetResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["assetId"] = new OpenApiInteger(123),
                ["fileName"] = new OpenApiString("epic_gaming_moment.mp4"),
                ["originalFileName"] = new OpenApiString("VID_20240115_194532.mp4"),
                ["fileSizeBytes"] = new OpenApiLong(45678901),
                ["title"] = new OpenApiString("Epic Gaming Moment"),
                ["uploadedAt"] = new OpenApiDateTime(DateTime.UtcNow),
                ["viewCount"] = new OpenApiInteger(1337),
                ["user"] = new OpenApiObject
                {
                    ["userId"] = new OpenApiInteger(42),
                    ["username"] = new OpenApiString("ProGamer123")
                },
                ["videoMetadata"] = new OpenApiObject
                {
                    ["videoMetadataId"] = new OpenApiInteger(456),
                    ["durationSeconds"] = new OpenApiInteger(127),
                    ["width"] = new OpenApiInteger(1920),
                    ["height"] = new OpenApiInteger(1080),
                    ["frameRate"] = new OpenApiDouble(60.0),
                    ["codec"] = new OpenApiString("h264"),
                    ["bitrateKbps"] = new OpenApiInteger(8000),
                    ["audioCodec"] = new OpenApiString("aac")
                }
            };
        }
    }
}

// Register in Program.cs
builder.Services.AddSwaggerGen(options =>
{
    // ... existing config ...
    options.SchemaFilter<MediaAssetResponseSchemaFilter>();
});
```

---

### 5. Add Swagger UI Customization

```csharp
// Program.cs
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Media Asset Manager API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root (optional)

    // Customization
    options.DocumentTitle = "Media Asset Manager API Documentation";
    options.DocExpansion(DocExpansion.List);
    options.DefaultModelsExpandDepth(2);
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.ShowExtensions();

    // Enable "Try it out" by default
    options.EnableTryItOutByDefault();

    // Persist authorization
    options.EnablePersistAuthorization();
});
```

---

## Architecture Improvements

### 1. Implement Result Pattern (Avoid Exceptions for Control Flow)

**Current Problem:**

```csharp
public async Task<MediaAsset> CreateAsync(MediaAsset asset, int userId)
{
    if (string.IsNullOrWhiteSpace(asset.FileName))
        throw new ArgumentException("File name is required."); // Exception for validation ❌

    var duplicate = await repository.GetByContentHashAsync(asset.ContentHash);
    if (duplicate != null)
        throw new InvalidOperationException("Duplicate file exists."); // Exception for business logic ❌

    return await repository.AddAsync(asset);
}
```

**Better: Result Pattern**

```csharp
// MediaAssetManager.Core/Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value, null, ErrorType.None);
    public static Result<T> Failure(string error, ErrorType errorType = ErrorType.BusinessRule)
        => new(false, default, error, errorType);
}

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    BusinessRule,
    Unauthorized,
    External
}
```

```csharp
// Updated service
public async Task<Result<MediaAsset>> CreateAsync(MediaAsset asset, int userId)
{
    // Validation
    if (string.IsNullOrWhiteSpace(asset.FileName))
        return Result<MediaAsset>.Failure("File name is required.", ErrorType.Validation);

    // Business rule check
    var duplicate = await repository.GetByContentHashAsync(asset.ContentHash);
    if (duplicate != null)
        return Result<MediaAsset>.Failure(
            $"A file with this content already exists (AssetId: {duplicate.AssetId}).",
            ErrorType.BusinessRule);

    // Success path
    asset.UserId = userId;
    var created = await repository.AddAsync(asset);
    return Result<MediaAsset>.Success(created);
}
```

```csharp
// Controller handling
[HttpPost]
public async Task<ActionResult<MediaAssetResponse>> Create([FromBody] CreateMediaAssetRequest request)
{
    var result = await service.CreateAsync(request.ToEntity(), userId);

    if (!result.IsSuccess)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(new ErrorResponse { Message = result.Error }),
            ErrorType.BusinessRule => Conflict(new ErrorResponse { Message = result.Error }),
            ErrorType.NotFound => NotFound(new ErrorResponse { Message = result.Error }),
            ErrorType.Unauthorized => Forbid(),
            _ => StatusCode(500, new ErrorResponse { Message = "An error occurred" })
        };
    }

    return CreatedAtAction(nameof(GetById), new { id = result.Value!.AssetId }, result.Value.ToResponse());
}
```

**Benefits:**

- ✅ No exceptions for expected failures (faster)
- ✅ Explicit success/failure handling
- ✅ Type-safe error handling
- ✅ Better for async code
- ✅ Easier to test

---

### 2. Implement CQRS (Command Query Responsibility Segregation)

Separate read and write operations for better performance and scalability.

**Current:** Service methods do both reads and writes

**Better:** Separate commands (writes) from queries (reads)

```bash
MediaAssetManager.Application/
├── Commands/
│   ├── CreateMediaAsset/
│   │   ├── CreateMediaAssetCommand.cs
│   │   ├── CreateMediaAssetCommandHandler.cs
│   │   └── CreateMediaAssetCommandValidator.cs
│   └── UpdateMediaAsset/
│       ├── UpdateMediaAssetCommand.cs
│       └── UpdateMediaAssetCommandHandler.cs
└── Queries/
    ├── GetMediaAssets/
    │   ├── GetMediaAssetsQuery.cs
    │   ├── GetMediaAssetsQueryHandler.cs
    │   └── MediaAssetDto.cs
    └── GetMediaAssetById/
        ├── GetMediaAssetByIdQuery.cs
        └── GetMediaAssetByIdQueryHandler.cs
```

**Using MediatR:**

```bash
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
```

```csharp
// Query
public record GetMediaAssetByIdQuery(int AssetId, HashSet<string>? Expand) : IRequest<Result<MediaAssetDto>>;

// Handler
public class GetMediaAssetByIdQueryHandler : IRequestHandler<GetMediaAssetByIdQuery, Result<MediaAssetDto>>
{
    private readonly MediaAssetContext _context;

    public async Task<Result<MediaAssetDto>> Handle(GetMediaAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var asset = await _context.MediaAssets
            .AsNoTracking()
            .Where(a => a.AssetId == request.AssetId)
            .Select(a => new MediaAssetDto
            {
                AssetId = a.AssetId,
                // ... projection
            })
            .FirstOrDefaultAsync(cancellationToken);

        return asset == null
            ? Result<MediaAssetDto>.Failure("Asset not found", ErrorType.NotFound)
            : Result<MediaAssetDto>.Success(asset);
    }
}

// Controller
[HttpGet("{id}")]
public async Task<ActionResult<MediaAssetDto>> GetById(int id, [FromQuery] string[]? expand)
{
    var query = new GetMediaAssetByIdQuery(id, expand?.ToHashSet());
    var result = await _mediator.Send(query);

    return result.IsSuccess ? Ok(result.Value) : NotFound();
}
```

**Benefits:**

- ✅ Optimized read models (use Select projections)
- ✅ Separate validation per operation
- ✅ Easier to add caching to queries
- ✅ Can use different data stores (read replicas)
- ✅ Cleaner controller code

---

### 3. Add Domain Events

```csharp
// MediaAssetManager.Core/Events/MediaAssetUploadedEvent.cs
public record MediaAssetUploadedEvent(int AssetId, int UserId, string FileName);

// Handler
public class MediaAssetUploadedEventHandler : INotificationHandler<MediaAssetUploadedEvent>
{
    private readonly IAnalyticsService _analytics;
    private readonly INotificationService _notifications;

    public async Task Handle(MediaAssetUploadedEvent notification, CancellationToken cancellationToken)
    {
        // Track analytics
        await _analytics.TrackUploadAsync(notification.AssetId, notification.UserId);

        // Send notification (optional)
        // await _notifications.NotifyFollowersAsync(notification.UserId, notification.AssetId);
    }
}

// Publish event
await _mediator.Publish(new MediaAssetUploadedEvent(asset.AssetId, userId, asset.FileName));
```

---

## Testing Strategy

### 1. Unit Tests Structure

```bash
MediaAssetManager.Tests/
├── Unit/
│   ├── Services/
│   │   ├── MediaAssetServiceTests.cs
│   │   └── PlaylistServiceTests.cs
│   ├── Repositories/
│   │   └── MediaAssetRepositoryTests.cs
│   └── Validators/
│       └── MediaAssetValidatorTests.cs
├── Integration/
│   ├── Controllers/
│   │   └── MediaAssetsControllerTests.cs
│   └── Database/
│       └── MediaAssetRepositoryIntegrationTests.cs
└── TestUtilities/
    ├── Fixtures/
    └── Builders/
```

**Example Unit Test:**

```csharp
// MediaAssetManager.Tests/Unit/Services/MediaAssetServiceTests.cs
public class MediaAssetServiceTests
{
    private readonly Mock<IMediaAssetRepository> _mockRepository;
    private readonly MediaAssetService _service;

    public MediaAssetServiceTests()
    {
        _mockRepository = new Mock<IMediaAssetRepository>();
        _service = new MediaAssetService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidAsset_ReturnsSuccess()
    {
        // Arrange
        var asset = new MediaAsset
        {
            FileName = "test.mp4",
            Title = "Test Video",
            FileSizeBytes = 1000,
            ContentHash = "abc123"
        };

        _mockRepository
            .Setup(r => r.GetByContentHashAsync(asset.ContentHash, null))
            .ReturnsAsync((MediaAsset?)null);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<MediaAsset>()))
            .ReturnsAsync(asset);

        // Act
        var result = await _service.CreateAsync(asset, userId: 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(asset.FileName, result.Value.FileName);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<MediaAsset>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateHash_ReturnsFailure()
    {
        // Arrange
        var existingAsset = new MediaAsset { AssetId = 99, ContentHash = "abc123" };
        var newAsset = new MediaAsset { ContentHash = "abc123", FileName = "duplicate.mp4" };

        _mockRepository
            .Setup(r => r.GetByContentHashAsync(newAsset.ContentHash, null))
            .ReturnsAsync(existingAsset);

        // Act
        var result = await _service.CreateAsync(newAsset, userId: 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BusinessRule, result.ErrorType);
        Assert.Contains("already exists", result.Error);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<MediaAsset>()), Times.Never);
    }
}
```

---

### 2. Integration Tests with WebApplicationFactory

```csharp
// MediaAssetManager.Tests/Integration/MediaAssetsControllerTests.cs
public class MediaAssetsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MediaAssetsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace PostgreSQL with in-memory database
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MediaAssetContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<MediaAssetContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task GetMediaAssets_ReturnsOkWithPaginatedResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/mediaassets?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<MediaAssetResponse>>(json);

        Assert.NotNull(result);
        Assert.True(result.PageSize <= 10);
    }

    [Fact]
    public async Task GetMediaAssets_WithInvalidExpand_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/mediaassets?expand=invalidField");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

---

## Authentication & Security

### Implement JWT Authentication

```csharp
// NuGet packages
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
```

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// Middleware order matters!
app.UseAuthentication();
app.UseAuthorization();
```

```csharp
// AuthController.cs
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    var user = await _userService.ValidateCredentialsAsync(request.Username, request.Password);
    if (user == null)
        return Unauthorized(new ErrorResponse { Message = "Invalid credentials" });

    var token = GenerateJwtToken(user);
    return Ok(new LoginResponse { Token = token, ExpiresAt = DateTime.UtcNow.AddHours(24) });
}

private string GenerateJwtToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(24),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

```csharp
// Protect endpoints
[Authorize]
[HttpPost]
public async Task<ActionResult<MediaAssetResponse>> Create([FromBody] CreateMediaAssetRequest request)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    // ... create asset with userId
}

// Role-based authorization
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteAny(int id) { /* Admin can delete any asset */ }
```

---

## Video Processing Implementation

### Implement FFmpeg Integration

```csharp
// MediaAssetManager.Services/VideoProcessingService.cs
public class VideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;

    public async Task<VideoMetadata> ExtractMetadataAsync(string filePath)
    {
        try
        {
            var videoInfo = await FFProbe.AnalyseAsync(filePath);

            return new VideoMetadata
            {
                DurationSeconds = (int)videoInfo.Duration.TotalSeconds,
                Width = videoInfo.PrimaryVideoStream?.Width ?? 0,
                Height = videoInfo.PrimaryVideoStream?.Height ?? 0,
                FrameRate = videoInfo.PrimaryVideoStream?.FrameRate ?? 0,
                Codec = videoInfo.PrimaryVideoStream?.CodecName,
                BitrateKbps = (int?)(videoInfo.PrimaryVideoStream?.BitRate / 1000),
                AudioCodec = videoInfo.PrimaryAudioStream?.CodecName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata from {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> GenerateThumbnailAsync(string videoPath, int atSecond = 3)
    {
        var thumbnailPath = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");

        try
        {
            await FFMpeg.SnapshotAsync(videoPath, thumbnailPath,
                size: new Size(320, 180),
                captureTime: TimeSpan.FromSeconds(atSecond));

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {VideoPath}", videoPath);
            throw;
        }
    }

    public async Task<string> CompressVideoAsync(string inputPath, string outputPath, int targetHeight = 720)
    {
        try
        {
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithConstantRateFactor(23)
                    .WithAudioCodec(AudioCodec.Aac)
                    .WithVariableBitrate(4)
                    .WithFastStart()
                    .Scale(VideoSize.Hd720)
                    .WithSpeedPreset(Speed.Medium))
                .ProcessAsynchronously();

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress video {InputPath}", inputPath);
            throw;
        }
        finally
        {
            // Cleanup temp files
            if (File.Exists(inputPath))
                File.Delete(inputPath);
        }
    }
}
```

**Processing Pipeline:**

```csharp
// MediaAssetService.cs
public async Task<Result<MediaAsset>> ProcessAndUploadAsync(Stream fileStream, string fileName, int userId)
{
    var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(fileName));

    try
    {
        // 1. Save to temp file
        await using (var fileStreamDest = File.Create(tempPath))
        {
            await fileStream.CopyToAsync(fileStreamDest);
        }

        // 2. Extract metadata
        var metadata = await _videoProcessingService.ExtractMetadataAsync(tempPath);

        // 3. Generate thumbnail
        var thumbnailPath = await _videoProcessingService.GenerateThumbnailAsync(tempPath);
        var thumbnailUrl = await _storageService.UploadAsync(thumbnailPath, $"thumbnails/{Guid.NewGuid()}.jpg");

        // 4. Compress if needed (>100MB or >1080p)
        var fileInfo = new FileInfo(tempPath);
        string uploadPath = tempPath;

        if (fileInfo.Length > 100_000_000 || metadata.Height > 1080)
        {
            var compressedPath = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
            await _videoProcessingService.CompressVideoAsync(tempPath, compressedPath, targetHeight: 720);
            uploadPath = compressedPath;
        }

        // 5. Upload to B2
        var storageUrl = await _storageService.UploadAsync(uploadPath, $"videos/{userId}/{Guid.NewGuid()}.mp4");

        // 6. Calculate content hash
        var contentHash = await CalculateFileHashAsync(uploadPath);

        // 7. Create database entry
        var asset = new MediaAsset
        {
            UserId = userId,
            FileName = fileName,
            OriginalFileName = fileName,
            FileSizeBytes = new FileInfo(uploadPath).Length,
            StoragePath = storageUrl,
            ThumbnailPath = thumbnailUrl,
            ContentHash = contentHash,
            VideoMetadata = metadata,
            Status = ProcessingStatus.Completed
        };

        return await CreateAsync(asset, userId);
    }
    finally
    {
        // Cleanup all temp files
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
}
```

---

## Deployment Preparation

### 1. Add Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck<B2StorageHealthCheck>("b2_storage");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

```csharp
// B2StorageHealthCheck.cs
public class B2StorageHealthCheck : IHealthCheck
{
    private readonly IB2StorageService _storage;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            await _storage.TestConnectionAsync();
            return HealthCheckResult.Healthy("B2 storage is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("B2 storage is not accessible", ex);
        }
    }
}
```

---

### 2. Add Logging & Monitoring

```csharp
// Use Serilog with structured logging
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console()
        .WriteTo.File("logs/mediaassetmanager-.txt", rollingInterval: RollingInterval.Day);
});
```

---

### 3. Docker Support

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MediaAssetManager.sln", "./"]
COPY ["MediaAssetManager.API/MediaAssetManager.API.csproj", "MediaAssetManager.API/"]
COPY ["MediaAssetManager.Core/MediaAssetManager.Core.csproj", "MediaAssetManager.Core/"]
COPY ["MediaAssetManager.Infrastructure/MediaAssetManager.Infrastructure.csproj", "MediaAssetManager.Infrastructure/"]
COPY ["MediaAssetManager.Services/MediaAssetManager.Services.csproj", "MediaAssetManager.Services/"]
RUN dotnet restore

COPY . .
WORKDIR "/src/MediaAssetManager.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install FFmpeg
RUN apt-get update && apt-get install -y ffmpeg

ENTRYPOINT ["dotnet", "MediaAssetManager.API.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'

services:
    api:
        build: .
        ports:
            - '5000:80'
        environment:
            - ASPNETCORE_ENVIRONMENT=Production
            - ConnectionStrings__DefaultConnection=Host=postgres;Database=mediaassetmanager;Username=postgres;Password=yourpassword
        depends_on:
            - postgres

    postgres:
        image: postgres:16
        environment:
            POSTGRES_PASSWORD: yourpassword
            POSTGRES_DB: mediaassetmanager
        volumes:
            - postgres_data:/var/lib/postgresql/data
        ports:
            - '5432:5432'

volumes:
    postgres_data:
```

---

## Priority Roadmap

### Phase 1: Performance (Week 1-2)

1. ✅ Fix ViewCount performance (computed column + trigger)
2. ✅ Remove `Include(a => a.Views)` from repository
3. ✅ Add database indexes
4. ✅ Implement `Select()` projections for list queries

### Phase 2: Documentation (Week 2-3)

1. ✅ Enable XML documentation
2. ✅ Add Swagger operation filters
3. ✅ Add request/response examples
4. ✅ Customize Swagger UI

### Phase 3: Testing (Week 3-4)

1. ✅ Unit tests for services
2. ✅ Integration tests for API
3. ✅ Repository tests with in-memory DB

### Phase 4: Video Processing (Week 4-6)

1. ✅ Implement FFmpeg service
2. ✅ Add upload endpoint
3. ✅ Thumbnail generation
4. ✅ Optional compression

### Phase 5: Authentication (Week 6-7)

1. ✅ JWT authentication
2. ✅ User registration/login
3. ✅ Protect endpoints

### Phase 6: Production Ready (Week 7-8)

1. ✅ Health checks
2. ✅ Structured logging
3. ✅ Docker support
4. ✅ CI/CD pipeline

---

## Conclusion

This guide provides a comprehensive roadmap for taking your Media Asset Manager from its current state to a production-ready system. Focus on performance optimizations first (especially the ViewCount issue), then move to documentation and testing before tackling more complex features like video processing and authentication.

**Key Takeaways:**

- **Use Select() projections** over Include() for queries - 5-10x faster
- **Implement computed columns** for aggregate values like ViewCount
- **Add comprehensive Swagger documentation** for better developer experience
- **Write tests early** - they save time in the long run
- **Use Result pattern** instead of exceptions for control flow
- **Consider CQRS** for complex domains with different read/write patterns

Remember: **Premature optimization is the root of all evil**. Measure performance bottlenecks before optimizing. Start with the low-hanging fruit (ViewCount, basic indexes) and iterate based on actual usage patterns.
