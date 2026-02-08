# Gaming Clip Platform - Streamlined Implementation Guide (V2)

> **Project**: Media Asset Manager - Portfolio Edition  
> **Focus**: Core features, minimal B2 egress, achievable scope  
> **Philosophy**: Ship a working product, then iterate

---

## Key Changes from V1

### 🎯 **Addressing Your Concerns**

#### 1. **B2 Free Tier Friendly**

**Problem**: V1 downloaded videos from B2 for processing → high egress costs

**Solution**: **Process BEFORE uploading to B2**

- Upload flow: Client → API → Process locally → Upload final to B2
- Never download from B2 for processing
- Store only ONE version per video (compressed OR original)
- Thumbnails generated during initial processing

**B2 Free Tier Limits**:

- Storage: 10 GB
- Egress: 1 GB/day (only for user viewing, not processing)
- Transactions: Generous for typical usage

**With V2 approach**:

- ~100 clips at 100MB each = 10GB storage ✅
- Egress only from user views (with CDN caching) ✅
- One-time upload per clip ✅

#### 2. **Portfolio-Focused Scope**

**V1 Scope**: Production-ready SaaS (too ambitious)  
**V2 Scope**: Impressive portfolio piece (achievable)

**Cut Features**:

- ❌ Multi-quality encoding (Netflix-style)
- ❌ Preview GIFs
- ❌ Background job processing (use synchronous for MVP)
- ❌ Playlist reordering
- ❌ Multipart uploads
- ❌ Processing status tracking (simplified)

**Keep Core Features** (Portfolio Impact):

- ✅ Video metadata extraction (FFmpeg skills)
- ✅ Thumbnail generation (visual polish)
- ✅ Smart compression (720p target)
- ✅ OAuth authentication (security knowledge)
- ✅ Basic favorites & playlists (engagement)
- ✅ Search & filtering (query skills)
- ✅ CDN integration (performance awareness)
- ✅ **Content hash duplicate detection** (data integrity)
- ✅ **Analytics tracking with heatmaps** (insights & visualization)
- ✅ **Admin dashboard** (primary consumer for analytics)

---

## Simplified Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    UPLOAD FLOW (V2)                          │
└──────────────────────────────────────────────────────────────┘

   Client                API Server              B2 Storage
     │                       │                       │
     │  1. Upload video      │                       │
     ├──────────────────────>│                       │
     │                       │                       │
     │                       │  2. Save to temp      │
     │                       │     directory         │
     │                       │                       │
     │                       │  3. Extract metadata  │
     │                       │     (FFprobe)         │
     │                       │                       │
     │                       │  4. Generate thumbnail│
     │                       │     (FFmpeg)          │
     │                       │                       │
     │                       │  5. Compress video    │
     │                       │     (if needed)       │
     │                       │                       │
     │                       │  6. Upload to B2      │
     │                       ├──────────────────────>│
     │                       │                       │
     │                       │  7. Save metadata     │
     │                       │     to database       │
     │                       │                       │
     │  8. Return success    │                       │
     │<──────────────────────┤                       │
     │                       │                       │
     │                       │  9. Cleanup temp      │
     │                       │                       │
```

**Key Insight**: Everything happens in ONE request. No background jobs needed for MVP.

---

## 1. Simplified Entity Model

### Keep It Simple

```csharp
namespace MediaAssetManager.Core.Entities
{
    public class MediaAsset
    {
        // === CORE PROPERTIES ===
        public int AssetId { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        // === USER CONTENT ===
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GameTitle { get; set; }

        // === VIDEO METADATA ===
        public int DurationSeconds { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal FrameRate { get; set; }

        // === STORAGE ===
        public string StoragePath { get; set; } = string.Empty; // Single path
        public string? ThumbnailPath { get; set; }

        // === FLAGS ===
        public bool IsCompressed { get; set; } = false; // Track if we compressed it
        public bool IsPublic { get; set; } = true;

        // === TIMESTAMPS ===
        public DateTime UploadedAt { get; set; }
        public DateTime? LastViewedAt { get; set; }

        // === SIMPLE ANALYTICS ===
        public int ViewCount { get; set; } = 0;

        // === NAVIGATION ===
        public User User { get; set; } = null!;
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<PlaylistItem> PlaylistItems { get; set; } = new List<PlaylistItem>();

        // === COMPUTED ===
        public string ResolutionLabel => Height >= 1080 ? "1080p"
            : Height >= 720 ? "720p"
            : Height >= 480 ? "480p" : "SD";
    }

    // Simplified supporting entities
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // OAuth
        public string? ClientId { get; set; }
        public string? ClientSecretHash { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<MediaAsset> Assets { get; set; } = new List<MediaAsset>();
        public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
    }

    public class Tag
    {
        public int TagId { get; set; }
        public int AssetId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public MediaAsset Asset { get; set; } = null!;
    }

    public class Favorite
    {
        public int FavoriteId { get; set; }
        public int UserId { get; set; }
        public int AssetId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public MediaAsset Asset { get; set; } = null!;
    }

    public class Playlist
    {
        public int PlaylistId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public ICollection<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
    }

    public class PlaylistItem
    {
        public int PlaylistItemId { get; set; }
        public int PlaylistId { get; set; }
        public int AssetId { get; set; }
        public DateTime AddedAt { get; set; }

        public Playlist Playlist { get; set; } = null!;
        public MediaAsset Asset { get; set; } = null!;
    }

    // Analytics tracking for heatmaps
    public class AssetView
    {
        public long ViewId { get; set; }
        public int AssetId { get; set; }
        public int? UserId { get; set; } // Null for anonymous
        public DateTime ViewedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public MediaAsset Asset { get; set; } = null!;
        public User? User { get; set; }
    }
}
```

**Simplified from V1**:

- ❌ Removed: ProcessingStatus, SessionId, Codec details, AudioCodec, BitrateKbps
- ❌ Removed: Compressed vs Original paths (just one path)
- ❌ Removed: PreviewGif, CompressionRatio
- ❌ Removed: IsDeleted (hard delete is fine for portfolio)
- ❌ Removed: FavoriteCount, DownloadCount (can compute from relationships)
- ✅ Kept: Essential video metadata for display
- ✅ Kept: User engagement features (favorites, playlists)
- ✅ Kept: ContentHash for duplicate detection
- ✅ Kept: AssetView tracking for analytics heatmaps

---

## 2. Video Processing - Process Once, Upload Once

### 2.1 Streamlined Processing Service

**File**: `MediaAssetManager.Services/VideoProcessingService.cs`

```csharp
using FFMpegCore;
using MediaAssetManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MediaAssetManager.Services
{
    public interface IVideoProcessingService
    {
        /// <summary>
        /// Process video: extract metadata, generate thumbnail, optionally compress
        /// Returns paths to processed files
        /// </summary>
        Task<ProcessingResult> ProcessVideoAsync(
            string inputPath,
            bool shouldCompress = true);
    }

    public record ProcessingResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        // Metadata
        public int DurationSeconds { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public decimal FrameRate { get; init; }

        // Paths to processed files
        public string VideoPath { get; init; } = string.Empty; // Compressed or original
        public long VideoSizeBytes { get; init; }
        public bool WasCompressed { get; init; }

        public string ThumbnailPath { get; init; } = string.Empty;
        public long ThumbnailSizeBytes { get; init; }
    }

    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly ILogger<VideoProcessingService> _logger;

        // Compression settings
        private const int TARGET_HEIGHT = 720;
        private const int TARGET_BITRATE_KBPS = 2500;
        private const int SIZE_THRESHOLD_MB = 50; // Only compress if larger

        public VideoProcessingService(ILogger<VideoProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessingResult> ProcessVideoAsync(
            string inputPath,
            bool shouldCompress = true)
        {
            try
            {
                _logger.LogInformation("Processing video: {InputPath}", inputPath);

                // 1. Extract metadata
                var mediaInfo = await FFProbe.AnalyseAsync(inputPath);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault()
                    ?? throw new InvalidOperationException("No video stream found");

                var metadata = new
                {
                    DurationSeconds = (int)mediaInfo.Duration.TotalSeconds,
                    Width = videoStream.Width,
                    Height = videoStream.Height,
                    FrameRate = (decimal)videoStream.FrameRate,
                    FileSizeBytes = new FileInfo(inputPath).Length
                };

                _logger.LogInformation(
                    "Video metadata: {Width}x{Height}, {Duration}s, {SizeMB:F2} MB",
                    metadata.Width, metadata.Height, metadata.DurationSeconds,
                    metadata.FileSizeBytes / 1024.0 / 1024.0
                );

                // 2. Generate thumbnail
                var thumbnailPath = await GenerateThumbnailAsync(inputPath);

                // 3. Decide if compression is needed
                var fileSizeMB = metadata.FileSizeBytes / 1024.0 / 1024.0;
                var needsCompression = shouldCompress
                    && metadata.Height > TARGET_HEIGHT
                    && fileSizeMB > SIZE_THRESHOLD_MB;

                string finalVideoPath;
                long finalVideoSize;
                bool wasCompressed = false;

                if (needsCompression)
                {
                    _logger.LogInformation("Compressing video to 720p...");
                    var compressedPath = Path.Combine(
                        Path.GetTempPath(),
                        $"{Guid.NewGuid()}_compressed.mp4"
                    );

                    var success = await CompressVideoAsync(inputPath, compressedPath);

                    if (success)
                    {
                        finalVideoPath = compressedPath;
                        finalVideoSize = new FileInfo(compressedPath).Length;
                        wasCompressed = true;

                        _logger.LogInformation(
                            "Compression complete: {OriginalMB:F2} MB → {CompressedMB:F2} MB",
                            fileSizeMB,
                            finalVideoSize / 1024.0 / 1024.0
                        );
                    }
                    else
                    {
                        // Compression failed, use original
                        finalVideoPath = inputPath;
                        finalVideoSize = metadata.FileSizeBytes;
                    }
                }
                else
                {
                    // Use original
                    finalVideoPath = inputPath;
                    finalVideoSize = metadata.FileSizeBytes;
                    _logger.LogInformation("Using original video (no compression needed)");
                }

                return new ProcessingResult
                {
                    Success = true,
                    DurationSeconds = metadata.DurationSeconds,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    FrameRate = metadata.FrameRate,
                    VideoPath = finalVideoPath,
                    VideoSizeBytes = finalVideoSize,
                    WasCompressed = wasCompressed,
                    ThumbnailPath = thumbnailPath,
                    ThumbnailSizeBytes = new FileInfo(thumbnailPath).Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video processing failed for {InputPath}", inputPath);
                return new ProcessingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GenerateThumbnailAsync(string videoPath)
        {
            var thumbnailPath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}_thumb.jpg"
            );

            await FFMpeg.SnapshotAsync(
                videoPath,
                thumbnailPath,
                new Size(1280, 720),
                TimeSpan.FromSeconds(2) // Thumbnail at 2 seconds
            );

            return thumbnailPath;
        }

        private async Task<bool> CompressVideoAsync(string inputPath, string outputPath)
        {
            try
            {
                return await FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, overwrite: true, options => options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithConstantRateFactor(23) // Good quality/size balance
                        .WithVideoBitrate(TARGET_BITRATE_KBPS)
                        .WithVideoFilters(filterOptions => filterOptions
                            .Scale(height: TARGET_HEIGHT)
                        )
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithAudioBitrate(128)
                        .WithFastStart() // Enable streaming
                    )
                    .ProcessAsynchronously();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compression failed");
                return false;
            }
        }
    }
}
```

---

## 3. Upload Flow - All in One Request

### 3.1 Media Asset Service (Orchestration)

**File**: `MediaAssetManager.Services/MediaAssetService.cs`

```csharp
namespace MediaAssetManager.Services
{
    public interface IMediaAssetService
    {
        Task<MediaAsset> UploadAsync(
            Stream fileStream,
            string originalFileName,
            int userId,
            string title,
            string? description = null,
            string? gameTitle = null,
            List<string>? tags = null);

        Task<MediaAsset?> GetByIdAsync(int assetId);
        Task<PagedResult<MediaAsset>> QueryAsync(MediaAssetQuery query);
        Task<bool> DeleteAsync(int assetId, int userId);
        Task IncrementViewCountAsync(int assetId);
    }

    public class MediaAssetService : IMediaAssetService
    {
        private readonly IMediaAssetRepository _assetRepository;
        private readonly IStorageService _storageService;
        private readonly IVideoProcessingService _videoProcessingService;
        private readonly ILogger<MediaAssetService> _logger;

        public MediaAssetService(
            IMediaAssetRepository assetRepository,
            IStorageService storageService,
            IVideoProcessingService videoProcessingService,
            ILogger<MediaAssetService> logger)
        {
            _assetRepository = assetRepository;
            _storageService = storageService;
            _videoProcessingService = videoProcessingService;
            _logger = logger;
        }

        public async Task<MediaAsset> UploadAsync(
            Stream fileStream,
            string originalFileName,
            int userId,
            string title,
            string? description = null,
            string? gameTitle = null,
            List<string>? tags = null)
        {
            var tempVideoPath = string.Empty;
            var processedVideoPath = string.Empty;
            var thumbnailPath = string.Empty;

            try
            {
                _logger.LogInformation(
                    "Starting upload for user {UserId}: {FileName}",
                    userId, originalFileName
                );

                // 1. Save uploaded file to temp location & calculate hash
                tempVideoPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName)}"
                );

                string contentHash;
                await using (var tempFile = File.Create(tempVideoPath))
                {
                    // Calculate SHA256 while writing
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    var hashBuffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await fileStream.ReadAsync(hashBuffer)) > 0)
                    {
                        await tempFile.WriteAsync(hashBuffer.AsMemory(0, bytesRead));
                        sha256.TransformBlock(hashBuffer, 0, bytesRead, null, 0);
                    }
                    sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    contentHash = Convert.ToHexString(sha256.Hash!);
                }

                _logger.LogInformation("Saved to temp: {TempPath}, Hash: {Hash}", tempVideoPath, contentHash);

                // Check for duplicate
                var existingAsset = await _assetRepository.GetByContentHashAsync(contentHash);
                if (existingAsset != null)
                {
                    _logger.LogWarning("Duplicate detected: {Hash} already exists as AssetId {AssetId}",
                        contentHash, existingAsset.AssetId);
                    throw new InvalidOperationException(
                        $"Duplicate video detected. This clip already exists (ID: {existingAsset.AssetId})");
                }

                // 2. Process video (metadata + thumbnail + compression)
                var processingResult = await _videoProcessingService.ProcessVideoAsync(
                    tempVideoPath,
                    shouldCompress: true
                );

                if (!processingResult.Success)
                {
                    throw new InvalidOperationException(
                        $"Video processing failed: {processingResult.ErrorMessage}"
                    );
                }

                processedVideoPath = processingResult.VideoPath;
                thumbnailPath = processingResult.ThumbnailPath;

                // 3. Upload video to B2
                var videoStoragePath = $"videos/{userId}/{Guid.NewGuid()}.mp4";

                await using (var videoStream = File.OpenRead(processedVideoPath))
                {
                    var (_, videoFileName, _) = await _storageService.UploadFileAsync(
                        videoStream,
                        videoStoragePath,
                        "video/mp4"
                    );
                }

                _logger.LogInformation("Uploaded video to B2: {Path}", videoStoragePath);

                // 4. Upload thumbnail to B2
                var thumbnailStoragePath = $"thumbnails/{userId}/{Guid.NewGuid()}.jpg";

                await using (var thumbStream = File.OpenRead(thumbnailPath))
                {
                    var (_, thumbFileName, _) = await _storageService.UploadFileAsync(
                        thumbStream,
                        thumbnailStoragePath,
                        "image/jpeg"
                    );
                }

                _logger.LogInformation("Uploaded thumbnail to B2: {Path}", thumbnailStoragePath);

                // 5. Create database record
                var asset = new MediaAsset
                {
                    UserId = userId,
                    FileName = Path.GetFileName(videoStoragePath),
                    OriginalFileName = originalFileName,
                    FileSizeBytes = processingResult.VideoSizeBytes,
                    Title = title,
                    Description = description,
                    GameTitle = gameTitle,
                    ContentHash = contentHash,
                    DurationSeconds = processingResult.DurationSeconds,
                    Width = processingResult.Width,
                    Height = processingResult.Height,
                    FrameRate = processingResult.FrameRate,
                    StoragePath = videoStoragePath,
                    ThumbnailPath = thumbnailStoragePath,
                    IsCompressed = processingResult.WasCompressed,
                    IsPublic = true,
                    UploadedAt = DateTime.UtcNow,
                    ViewCount = 0
                };

                // Add tags if provided
                if (tags != null && tags.Any())
                {
                    foreach (var tagName in tags.Distinct())
                    {
                        asset.Tags.Add(new Tag
                        {
                            TagName = tagName.Trim().ToLowerInvariant()
                        });
                    }
                }

                var createdAsset = await _assetRepository.CreateAsync(asset);

                _logger.LogInformation(
                    "Upload complete: AssetId={AssetId}, Size={SizeMB:F2}MB, Compressed={IsCompressed}",
                    createdAsset.AssetId,
                    processingResult.VideoSizeBytes / 1024.0 / 1024.0,
                    processingResult.WasCompressed
                );

                return createdAsset;
            }
            finally
            {
                // 6. Cleanup temp files
                CleanupTempFile(tempVideoPath);

                if (processedVideoPath != tempVideoPath)
                {
                    CleanupTempFile(processedVideoPath);
                }

                CleanupTempFile(thumbnailPath);
            }
        }

        public async Task<bool> DeleteAsync(int assetId, int userId)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId);

            if (asset == null || asset.UserId != userId)
            {
                return false;
            }

            // Delete from B2
            await _storageService.DeleteFileAsync(null, asset.StoragePath);

            if (!string.IsNullOrEmpty(asset.ThumbnailPath))
            {
                await _storageService.DeleteFileAsync(null, asset.ThumbnailPath);
            }

            // Delete from database (cascade delete handles related records)
            await _assetRepository.DeleteAsync(assetId);

            _logger.LogInformation("Deleted asset {AssetId}", assetId);
            return true;
        }

        public async Task IncrementViewCountAsync(int assetId, int? userId = null, string? ipAddress = null, string? userAgent = null)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId);
            if (asset != null)
            {
                // Update aggregate counter
                asset.ViewCount++;
                asset.LastViewedAt = DateTime.UtcNow;
                await _assetRepository.UpdateAsync(asset);

                // Track individual view for analytics/heatmaps
                var view = new AssetView
                {
                    AssetId = assetId,
                    UserId = userId,
                    ViewedAt = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };
                await _assetRepository.TrackViewAsync(view);
            }
        }

        public async Task<MediaAsset?> GetByIdAsync(int assetId)
        {
            return await _assetRepository.GetByIdAsync(assetId);
        }

        public async Task<PagedResult<MediaAsset>> QueryAsync(MediaAssetQuery query)
        {
            return await _assetRepository.GetPagedAsync(query);
        }

        private void CleanupTempFile(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    _logger.LogDebug("Cleaned up temp file: {Path}", path);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temp file: {Path}", path);
                }
            }
        }
    }
}
```

**Key Points**:

- ✅ Everything in one transaction
- ✅ Videos processed locally (no B2 egress)
- ✅ Upload only final processed files
- ✅ Automatic cleanup on success or failure
- ✅ Simple error handling

---

## 4. Simplified API Controller

**File**: `MediaAssetManager.API/Controllers/MediaAssetsController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class MediaAssetsController : ControllerBase
{
    private readonly IMediaAssetService _assetService;
    private readonly IStorageService _storageService;
    private readonly ILogger<MediaAssetsController> _logger;

    public MediaAssetsController(
        IMediaAssetService assetService,
        IStorageService storageService,
        ILogger<MediaAssetsController> logger)
    {
        _assetService = assetService;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a gaming clip
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(1_073_741_824)] // 1GB max
    public async Task<ActionResult<MediaAssetResponse>> Upload(
        [FromForm] UploadRequest request,
        IFormFile file)
    {
        try
        {
            // Validation
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { error = $"Unsupported format. Allowed: {string.Join(", ", allowedExtensions)}" });

            if (file.Length > 1_073_741_824) // 1GB
                return BadRequest(new { error = "File too large (max 1GB)" });

            var userId = GetCurrentUserId();

            // Upload & process
            await using var stream = file.OpenReadStream();
            var asset = await _assetService.UploadAsync(
                stream,
                file.FileName,
                userId,
                request.Title ?? file.FileName,
                request.Description,
                request.GameTitle,
                request.Tags
            );

            return CreatedAtAction(
                nameof(GetById),
                new { id = asset.AssetId },
                asset.ToResponse()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            return StatusCode(500, new { error = "Upload processing failed" });
        }
    }

    /// <summary>
    /// Get clip by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MediaAssetResponse>> GetById(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return NotFound();

        // Check permissions
        if (!asset.IsPublic && asset.UserId != GetCurrentUserId())
            return Forbid();

        return Ok(asset.ToResponse());
    }

    /// <summary>
    /// Stream/download video
    /// </summary>
    [HttpGet("{id}/stream")]
    public async Task<IActionResult> Stream(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return NotFound();

        if (!asset.IsPublic && asset.UserId != GetCurrentUserId())
            return Forbid();

        // Track view with context
        await _assetService.IncrementViewCountAsync(
            id,
            GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()
        );

        // Generate signed URL and redirect
        var signedUrl = await _storageService.GetSignedDownloadUrlAsync(
            asset.StoragePath,
            expirationSeconds: 3600
        );

        return Redirect(signedUrl);
    }

    /// <summary>
    /// Get thumbnail
    /// </summary>
    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset?.ThumbnailPath == null) return NotFound();

        var signedUrl = await _storageService.GetSignedDownloadUrlAsync(
            asset.ThumbnailPath,
            expirationSeconds: 86400 // 24 hours for thumbnails
        );

        return Redirect(signedUrl);
    }

    /// <summary>
    /// Search clips
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> Search(
        [FromQuery] MediaAssetQueryRequest request)
    {
        var query = request.ToQuery();
        var result = await _assetService.QueryAsync(query);

        return Ok(new PaginatedResponse<MediaAssetResponse>
        {
            Items = result.Items.Select(a => a.ToResponse()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }

    /// <summary>
    /// Delete clip
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var success = await _assetService.DeleteAsync(id, userId);

        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Get my clips
    /// </summary>
    [HttpGet("my-clips")]
    [Authorize]
    public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> GetMyClips(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var query = new MediaAssetQuery
        {
            UserId = userId,
            PageNumber = page,
            PageSize = pageSize,
            SortBy = MediaAssetSortBy.UploadedAt,
            SortDescending = true
        };

        var result = await _assetService.QueryAsync(query);
        return Ok(result.ToPaginatedResponse());
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}

public record UploadRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? GameTitle { get; init; }
    public List<string>? Tags { get; init; }
}
```

---

## 5. Simplified Query System

**File**: `MediaAssetManager.Core/Queries/MediaAssetQuery.cs`

```csharp
namespace MediaAssetManager.Core.Queries
{
    public class MediaAssetQuery
    {
        // Text search
        public string? SearchQuery { get; set; } // Search title, description, game
        public string? GameTitle { get; set; }
        public List<string>? Tags { get; set; }

        // User filter
        public int? UserId { get; set; }
        public bool? IsPublic { get; set; }

        // Video filters
        public int? MinDurationSeconds { get; set; }
        public int? MaxDurationSeconds { get; set; }
        public int? MinHeight { get; set; } // e.g., 720 for HD+

        // Date filters
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }

        // Sorting
        public MediaAssetSortBy SortBy { get; set; } = MediaAssetSortBy.UploadedAt;
        public bool SortDescending { get; set; } = true;

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public enum MediaAssetSortBy
    {
        UploadedAt,
        Title,
        Duration,
        ViewCount,
        GameTitle
    }
}
```

---

## 6. B2 + Cloudflare CDN (Free!)

### Setup Steps

1. **Point domain to B2 via Cloudflare**:

    ```
    DNS Record (CNAME):
    cdn.yourdomain.com → s3.eu-central-003.backblazeb2.com

    Enable Cloudflare Proxy (Orange Cloud)
    ```

2. **Cloudflare Page Rule**:

    ```
    URL: cdn.yourdomain.com/*
    Settings:
      - Cache Level: Cache Everything
      - Edge Cache TTL: 1 month
    ```

3. **Update appsettings.json**:

    ```json
    {
    	"B2": {
    		"KeyId": "<secret>",
    		"KeySecret": "<secret>",
    		"BucketName": "media-asset-manager-dev",
    		"Endpoint": "https://s3.eu-central-003.backblazeb2.com",
    		"CdnBaseUrl": "https://cdn.yourdomain.com"
    	}
    }
    ```

4. **Storage Service CDN Support**:
    ```csharp
    public string GetCdnUrl(string storagePath)
    {
        // Public CDN URL (cached by Cloudflare, no egress cost!)
        return $"{_cdnBaseUrl}/{_bucketName}/{storagePath}";
    }
    ```

**Cost with CDN**:

- Storage: 10GB × $0.006/GB = **$0.06/month**
- Egress via Cloudflare: **$0** (Bandwidth Alliance)
- **Total: ~$0.06/month** 🎉

---

## 7. Authentication (Keep from V1)

Use the same OAuth client/secret approach from V1 - it's solid and demonstrates security knowledge for portfolio.

**Quick Reference**:

```csharp
// Generate credentials for a user
POST /api/auth/credentials
{ "userId": 1 }
→ { "clientId": "...", "clientSecret": "..." }

// Exchange for access token
POST /api/auth/token
{ "clientId": "...", "clientSecret": "..." }
→ { "accessToken": "...", "expiresIn": 3600 }

// Use token
GET /api/mediaassets
Authorization: Bearer {accessToken}
```

---

## 8. Favorites & Playlists (Simplified)

Keep the core service layer from V1 but simplified:

```csharp
// Favorites
POST /api/favorites/{assetId}    → Add favorite
DELETE /api/favorites/{assetId}  → Remove favorite
GET /api/favorites               → Get my favorites

// Playlists
POST /api/playlists                      → Create playlist
POST /api/playlists/{id}/assets/{assetId} → Add to playlist
GET /api/playlists/{id}/assets           → Get playlist items
DELETE /api/playlists/{id}               → Delete playlist
```

**Skip**: Playlist reordering, sort order management (nice-to-have, not MVP)

---

## 9. Admin Dashboard & Analytics

### 9.1 Analytics Service with Heatmaps

**File**: `MediaAssetManager.Services/AnalyticsService.cs`

```csharp
namespace MediaAssetManager.Services
{
    public interface IAnalyticsService
    {
        Task<AdminDashboardStats> GetAdminDashboardStatsAsync();
        Task<Dictionary<DateTime, int>> GetUploadHeatmapAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<DateTime, int>> GetViewHeatmapAsync(DateTime startDate, DateTime endDate);
        Task<List<TopAsset>> GetTopAssetsAsync(int count = 10);
        Task<StorageStats> GetStorageStatsAsync();
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly IMediaAssetRepository _assetRepository;
        private readonly IAssetViewRepository _viewRepository;
        private readonly IUserRepository _userRepository;

        public AnalyticsService(
            IMediaAssetRepository assetRepository,
            IAssetViewRepository viewRepository,
            IUserRepository userRepository)
        {
            _assetRepository = assetRepository;
            _viewRepository = viewRepository;
            _userRepository = userRepository;
        }

        public async Task<AdminDashboardStats> GetAdminDashboardStatsAsync()
        {
            var totalAssets = await _assetRepository.CountAsync();
            var totalUsers = await _userRepository.CountAsync();
            var totalViews = await _viewRepository.CountAsync();
            var totalStorage = await _assetRepository.SumFileSizesAsync();

            var last30Days = DateTime.UtcNow.AddDays(-30);
            var recentUploads = await _assetRepository.CountSinceAsync(last30Days);
            var recentViews = await _viewRepository.CountSinceAsync(last30Days);

            return new AdminDashboardStats
            {
                TotalAssets = totalAssets,
                TotalUsers = totalUsers,
                TotalViews = totalViews,
                TotalStorageBytes = totalStorage,
                TotalStorageGB = totalStorage / 1024.0 / 1024.0 / 1024.0,
                UploadsLast30Days = recentUploads,
                ViewsLast30Days = recentViews,
                AverageViewsPerAsset = totalAssets > 0 ? (double)totalViews / totalAssets : 0
            };
        }

        public async Task<Dictionary<DateTime, int>> GetUploadHeatmapAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var uploads = await _assetRepository.GetUploadsByDateRangeAsync(
                startDate,
                endDate
            );

            var heatmap = uploads
                .GroupBy(a => a.UploadedAt.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            return FillMissingDates(heatmap, startDate, endDate);
        }

        public async Task<Dictionary<DateTime, int>> GetViewHeatmapAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var views = await _viewRepository.GetViewsByDateRangeAsync(
                startDate,
                endDate
            );

            var heatmap = views
                .GroupBy(v => v.ViewedAt.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            return FillMissingDates(heatmap, startDate, endDate);
        }

        public async Task<List<TopAsset>> GetTopAssetsAsync(int count = 10)
        {
            var topAssets = await _assetRepository.GetTopByViewCountAsync(count);

            return topAssets.Select(a => new TopAsset
            {
                AssetId = a.AssetId,
                Title = a.Title,
                GameTitle = a.GameTitle,
                ViewCount = a.ViewCount,
                UploadedAt = a.UploadedAt,
                DurationSeconds = a.DurationSeconds,
                FileSizeMB = a.FileSizeBytes / 1024.0 / 1024.0
            }).ToList();
        }

        public async Task<StorageStats> GetStorageStatsAsync()
        {
            var allAssets = await _assetRepository.GetAllAsync();

            var totalOriginalSize = allAssets.Sum(a => a.FileSizeBytes);
            var compressedAssets = allAssets.Where(a => a.IsCompressed).ToList();
            var compressionSavings = compressedAssets.Any()
                ? compressedAssets.Sum(a => a.FileSizeBytes) * 0.5 // Estimate 50% savings
                : 0;

            var byGame = allAssets
                .Where(a => !string.IsNullOrEmpty(a.GameTitle))
                .GroupBy(a => a.GameTitle)
                .Select(g => new GameStorageStats
                {
                    GameTitle = g.Key!,
                    AssetCount = g.Count(),
                    TotalSizeBytes = g.Sum(a => a.FileSizeBytes),
                    TotalSizeGB = g.Sum(a => a.FileSizeBytes) / 1024.0 / 1024.0 / 1024.0
                })
                .OrderByDescending(g => g.TotalSizeBytes)
                .ToList();

            return new StorageStats
            {
                TotalStorageBytes = totalOriginalSize,
                TotalStorageGB = totalOriginalSize / 1024.0 / 1024.0 / 1024.0,
                CompressedAssetCount = compressedAssets.Count,
                EstimatedSavingsBytes = (long)compressionSavings,
                EstimatedSavingsGB = compressionSavings / 1024.0 / 1024.0 / 1024.0,
                StorageByGame = byGame
            };
        }

        private Dictionary<DateTime, int> FillMissingDates(
            Dictionary<DateTime, int> data,
            DateTime startDate,
            DateTime endDate)
        {
            var result = new Dictionary<DateTime, int>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                result[date] = data.GetValueOrDefault(date, 0);
            }

            return result;
        }
    }

    // DTOs
    public record AdminDashboardStats
    {
        public int TotalAssets { get; init; }
        public int TotalUsers { get; init; }
        public long TotalViews { get; init; }
        public long TotalStorageBytes { get; init; }
        public double TotalStorageGB { get; init; }
        public int UploadsLast30Days { get; init; }
        public long ViewsLast30Days { get; init; }
        public double AverageViewsPerAsset { get; init; }
    }

    public record TopAsset
    {
        public int AssetId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? GameTitle { get; init; }
        public int ViewCount { get; init; }
        public DateTime UploadedAt { get; init; }
        public int DurationSeconds { get; init; }
        public double FileSizeMB { get; init; }
    }

    public record StorageStats
    {
        public long TotalStorageBytes { get; init; }
        public double TotalStorageGB { get; init; }
        public int CompressedAssetCount { get; init; }
        public long EstimatedSavingsBytes { get; init; }
        public double EstimatedSavingsGB { get; init; }
        public List<GameStorageStats> StorageByGame { get; init; } = new();
    }

    public record GameStorageStats
    {
        public string GameTitle { get; init; } = string.Empty;
        public int AssetCount { get; init; }
        public long TotalSizeBytes { get; init; }
        public double TotalSizeGB { get; init; }
    }
}
```

### 9.2 Admin Dashboard Controller

**File**: `MediaAssetManager.API/Controllers/AdminController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")] // Require admin role
public class AdminController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AdminController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get admin dashboard overview stats
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardStats>> GetDashboard()
    {
        var stats = await _analyticsService.GetAdminDashboardStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Get upload activity heatmap (GitHub-style)
    /// </summary>
    [HttpGet("heatmap/uploads")]
    public async Task<ActionResult<HeatmapResponse>> GetUploadHeatmap(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddYears(-1);
        var end = endDate ?? DateTime.UtcNow;

        var data = await _analyticsService.GetUploadHeatmapAsync(start, end);

        return Ok(new HeatmapResponse
        {
            StartDate = start,
            EndDate = end,
            Data = data
        });
    }

    /// <summary>
    /// Get view activity heatmap
    /// </summary>
    [HttpGet("heatmap/views")]
    public async Task<ActionResult<HeatmapResponse>> GetViewHeatmap(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddYears(-1);
        var end = endDate ?? DateTime.UtcNow;

        var data = await _analyticsService.GetViewHeatmapAsync(start, end);

        return Ok(new HeatmapResponse
        {
            StartDate = start,
            EndDate = end,
            Data = data
        });
    }

    /// <summary>
    /// Get top performing assets
    /// </summary>
    [HttpGet("top-assets")]
    public async Task<ActionResult<List<TopAsset>>> GetTopAssets([FromQuery] int count = 10)
    {
        var topAssets = await _analyticsService.GetTopAssetsAsync(count);
        return Ok(topAssets);
    }

    /// <summary>
    /// Get storage statistics
    /// </summary>
    [HttpGet("storage")]
    public async Task<ActionResult<StorageStats>> GetStorageStats()
    {
        var stats = await _analyticsService.GetStorageStatsAsync();
        return Ok(stats);
    }
}

public record HeatmapResponse
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public Dictionary<DateTime, int> Data { get; init; } = new();
}
```

### 9.3 Database Indexes for Analytics Performance

Add to `MediaAssetContext.OnModelCreating`:

```csharp
// Analytics indexes
modelBuilder.Entity<AssetView>()
    .HasIndex(v => v.ViewedAt);

modelBuilder.Entity<AssetView>()
    .HasIndex(v => new { v.AssetId, v.ViewedAt });

modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => a.UploadedAt);

modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => a.ContentHash)
    .IsUnique(); // Enforce duplicate detection at DB level
```

---

## Implementation Roadmap (V2 - Realistic)

### Week 1-2: Core Upload Flow

- [ ] Entity models (simplified)
- [ ] Database migration
- [ ] Video processing service (FFmpeg)
- [ ] Upload endpoint with processing
- [ ] Test full upload → process → store → retrieve flow

### Week 3: Query & Display

- [ ] Query system with filters
- [ ] Search endpoint
- [ ] Thumbnail generation & serving
- [ ] Basic frontend for upload/view

### Week 4: Authentication

- [ ] User entity & registration
- [ ] OAuth client/secret generation
- [ ] JWT token service
- [ ] Protect endpoints with [Authorize]

### Week 5: Analytics & Admin Dashboard

- [ ] AssetView entity & repository
- [ ] Analytics service (heatmaps, stats)
- [ ] Admin dashboard controller
- [ ] Content hash duplicate detection
- [ ] Storage statistics

### Week 6: Social Features

- [ ] Favorites (add/remove/list)
- [ ] Playlists (create/add/list)
- [ ] Tag system
- [ ] Game-based filtering

### Week 7: CDN & Polish

- [ ] Cloudflare CDN setup
- [ ] Delete cascade
- [ ] Error handling & validation
- [ ] API documentation (Swagger)
- [ ] Frontend admin dashboard

**Total: 7 weeks vs. 10 weeks in V1** ✅

---

## What Makes This Portfolio-Ready?

### Technical Skills Demonstrated

1. **Backend Architecture**
    - Clean Architecture (separation of concerns)
    - Repository pattern
    - Service layer orchestration
    - Dependency injection

2. **External Integrations**
    - S3-compatible storage (B2)
    - FFmpeg video processing
    - CDN integration (Cloudflare)

3. **Video Processing**
    - Metadata extraction
    - Thumbnail generation
    - Video compression
    - Format handling

4. **Authentication & Security**
    - OAuth 2.0 client credentials flow
    - JWT tokens
    - Role-based access control
    - Secure credential storage

5. **Database Design**
    - Normalized schema
    - Relationships (1:N, M:N)
    - Indexing strategy
    - EF Core migrations

6. **API Design**
    - RESTful endpoints
    - Pagination
    - Query parameters
    - Error handling
    - Swagger documentation

7. **Performance**
    - CDN for static assets
    - Video compression (cost-conscious)
    - Efficient queries
    - Streaming (no buffering)

8. **Analytics & Visualization**
    - Aggregate statistics
    - Heatmap generation (GitHub-style)
    - Storage tracking
    - Performance metrics
    - Admin dashboard

---

## Cost Comparison: V1 vs V2

### V1 (Background Processing)

```
Scenario: 100 clips uploaded
- Initial upload: 100 × 100MB = 10GB → $0.00 (upload free)
- Download for processing: 10GB × $0.01/GB = $0.10
- Re-upload compressed: 5GB × $0.00 = $0.00 (upload free)
- User views (with CDN): $0.00 (Cloudflare)
Total B2 cost: $0.10 for processing egress
```

### V2 (Local Processing)

```
Scenario: 100 clips uploaded
- Initial upload (already processed): 5GB × $0.00 = $0.00
- User views (with CDN): $0.00 (Cloudflare)
Total B2 cost: $0.00 ✅
```

**For free tier development**: V2 is sustainable indefinitely! 🎉

---

## Future Enhancements (Post-Portfolio)

If you decide to extend this later:

1. **Background Processing** (Hangfire)
    - For async uploads
    - Email notifications
    - Batch operations

2. **Advanced Analytics**
    - Heatmaps
    - Event tracking
    - User engagement metrics

3. **Multi-Quality Encoding**
    - Adaptive streaming (HLS/DASH)
    - Multiple resolutions
    - Bandwidth optimization

4. **Social Features**
    - Comments & reactions
    - Sharing
    - User profiles

5. **Admin Dashboard**
    - Content moderation
    - User management
    - Storage analytics

---

## Development Environment Setup

### Prerequisites

1. **Install FFmpeg**:

    ```bash
    # Windows (Chocolatey)
    choco install ffmpeg

    # Or download from: https://ffmpeg.org/download.html
    ```

2. **NuGet Packages**:

    ```bash
    # Processing
    dotnet add package FFMpegCore --version 5.1.0

    # Storage
    dotnet add package AWSSDK.S3 --version 3.7.0

    # Authentication
    dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
    dotnet add package BCrypt.Net-Next --version 4.0.3

    # Database
    dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
    ```

3. **Environment Variables** (for secrets):
    ```json
    // appsettings.Development.json
    {
    	"ConnectionStrings": {
    		"DefaultConnection": "Host=localhost;Database=mediaassets;Username=postgres;Password=..."
    	},
    	"B2": {
    		"KeyId": "your-b2-key-id",
    		"KeySecret": "your-b2-key-secret",
    		"BucketName": "media-asset-manager-dev",
    		"Endpoint": "https://s3.eu-central-003.backblazeb2.com",
    		"CdnBaseUrl": "https://localhost:5001" // Local dev, no CDN yet
    	},
    	"JwtSettings": {
    		"SecretKey": "your-256-bit-secret-key-here-minimum-32-characters!",
    		"Issuer": "MediaAssetManager",
    		"Audience": "MediaAssetManagerAPI"
    	}
    }
    ```

---

## Testing the Upload Flow

### Manual Test

1. **Generate client credentials**:

    ```bash
    curl -X POST https://localhost:5001/api/auth/credentials \
      -H "Content-Type: application/json" \
      -d '{"userId": 1}'
    ```

2. **Get access token**:

    ```bash
    curl -X POST https://localhost:5001/api/auth/token \
      -H "Content-Type: application/json" \
      -d '{"clientId":"...", "clientSecret":"..."}'
    ```

3. **Upload video**:

    ```bash
    curl -X POST https://localhost:5001/api/mediaassets/upload \
      -H "Authorization: Bearer {token}" \
      -F "file=@gameplay.mp4" \
      -F "title=Epic Pentakill" \
      -F "gameTitle=Valorant" \
      -F "tags=pentakill,valorant,clutch"
    ```

4. **Check result** (wait for processing to complete)

5. **Stream video**:
    ```bash
    curl https://localhost:5001/api/mediaassets/{assetId}/stream
    # Should redirect to signed B2 URL
    ```

---

## Summary: V1 vs V2

| Feature              | V1 (Production SaaS)           | V2 (Portfolio MVP)         |
| -------------------- | ------------------------------ | -------------------------- |
| **Scope**            | Comprehensive                  | Essential                  |
| **Timeline**         | 10 weeks                       | 7 weeks                    |
| **B2 Egress**        | High (download for processing) | Minimal (local processing) |
| **Processing**       | Background (Hangfire)          | Synchronous                |
| **Video Variants**   | Multi-quality                  | Single optimized           |
| **Analytics**        | Heatmaps, detailed tracking    | Basic view counts          |
| **Entity Model**     | 15+ properties                 | 12 properties              |
| **Complexity**       | High                           | Moderate                   |
| **Portfolio Impact** | ⭐⭐⭐⭐⭐                     | ⭐⭐⭐⭐⭐ (same!)         |

**Verdict**: V2 achieves the same portfolio impact with 40% less time and zero B2 costs during development! 🚀

---

## Questions?

- **Q: Can I upgrade from V2 to V1 later?**
    - A: Yes! V2 is a subset of V1. Add Hangfire, expand entity model, implement background processing.

- **Q: What if processing takes too long?**
    - A: Add timeout (e.g., 5 minutes). For larger files, return 202 Accepted and process async.

- **Q: How to handle concurrent uploads?**
    - A: Temp files use GUID names (no collision). B2 handles concurrent uploads fine.

- **Q: Should I use Docker?**
    - A: For portfolio, not required. For deployment, yes (include FFmpeg in image).

---

**Ready to start? Begin with Week 1-2 tasks!** 🎮🎬
