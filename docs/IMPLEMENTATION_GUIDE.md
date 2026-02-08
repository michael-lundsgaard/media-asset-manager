# Gaming Clip Platform - Implementation Guide

> **Project**: Media Asset Manager - Gaming Clip Edition  
> **Architecture**: Clean Architecture (.NET 8)  
> **Storage**: Backblaze B2 (S3-compatible)  
> **Database**: PostgreSQL with EF Core  
> **Target**: Gaming session clip management with video processing

---

## Table of Contents

1. [Entity Model Extensions](#1-entity-model-extensions)
2. [Video Processing Services](#2-video-processing-services)
3. [Background Orchestration & Transactions](#3-background-orchestration--transactions)
4. [API Extensions](#4-api-extensions)
5. [B2 Bucket Organization](#5-b2-bucket-organization)
6. [Advanced Query Object Extensions](#6-advanced-query-object-extensions)
7. [Video Quality Control](#7-video-quality-control)
8. [Authentication System (Client/Secret OAuth)](#8-authentication-system-clientsecret-oauth)
9. [CDN Strategy (Cost-Effective)](#9-cdn-strategy-cost-effective)
10. [Favorites & Playlists](#10-favorites--playlists)
11. [Analytics & Tracking (Heatmaps)](#11-analytics--tracking-heatmaps)

---

## 1. Entity Model Extensions

### 1.1 Core Entities Overview

```
┌─────────────────┐       ┌──────────────────┐       ┌─────────────────┐
│   MediaAsset    │──────>│   VideoMetadata  │       │      User       │
│  (Main Entity)  │       │  (Video Specs)   │       │   (Owner)       │
└────────┬────────┘       └──────────────────┘       └────────┬────────┘
         │                                                     │
         │ 1:N                                           1:N   │
         │                                                     │
         ▼                                                     ▼
┌─────────────────┐       ┌──────────────────┐       ┌─────────────────┐
│   AssetTag      │       │  ProcessingJob   │       │   Playlist      │
│  (Many-to-Many) │       │ (Status Track)   │       │ (Collections)   │
└─────────────────┘       └──────────────────┘       └────────┬────────┘
                                                               │
         ┌─────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────┐       ┌──────────────────┐       ┌─────────────────┐
│ PlaylistAsset   │       │  AssetAnalytics  │       │    Favorite     │
│  (Join Table)   │       │   (Tracking)     │       │  (User Likes)   │
└─────────────────┘       └──────────────────┘       └─────────────────┘
```

### 1.2 Enhanced MediaAsset Entity

**File**: `MediaAssetManager.Core/Entities/MediaAsset.cs`

```csharp
namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// Represents a gaming clip or media asset uploaded to the platform
    /// </summary>
    public class MediaAsset
    {
        // === CORE PROPERTIES (EXISTING) ===
        public int AssetId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? Title { get; set; }
        public DateTime UploadedAt { get; set; }

        // === NEW: USER & OWNERSHIP ===
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // === NEW: CONTENT CLASSIFICATION ===
        public MediaType MediaType { get; set; } = MediaType.Video;
        public string? Description { get; set; }
        public string ContentHash { get; set; } = string.Empty; // SHA256 for duplicate detection

        // === NEW: GAMING CONTEXT ===
        public string? GameTitle { get; set; }
        public string? SessionId { get; set; } // Group clips from same session
        public DateTime? RecordedAt { get; set; } // Actual game recording time (vs upload time)

        // === NEW: VIDEO METADATA ===
        public int? DurationSeconds { get; set; }
        public int? Width { get; set; }  // Resolution width
        public int? Height { get; set; } // Resolution height
        public string? Codec { get; set; } // e.g., "h264", "hevc"
        public int? BitrateKbps { get; set; }
        public decimal? FrameRate { get; set; } // e.g., 60.0, 30.0
        public string? AudioCodec { get; set; }

        // === NEW: PROCESSING & STORAGE ===
        public ProcessingStatus Status { get; set; } = ProcessingStatus.Uploaded;
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessingError { get; set; }

        // Original file path in B2
        public string OriginalStoragePath { get; set; } = string.Empty;

        // Compressed version (if enabled)
        public string? CompressedStoragePath { get; set; }
        public long? CompressedFileSizeBytes { get; set; }
        public decimal? CompressionRatio { get; set; } // e.g., 0.45 = 45% of original

        // Thumbnail paths
        public string? ThumbnailStoragePath { get; set; }
        public string? PreviewGifStoragePath { get; set; }

        // === NEW: VISIBILITY & PERMISSIONS ===
        public bool IsPublic { get; set; } = true;
        public bool IsDeleted { get; set; } = false; // Soft delete
        public DateTime? DeletedAt { get; set; }

        // === NEW: ANALYTICS ===
        public int ViewCount { get; set; } = 0;
        public int DownloadCount { get; set; } = 0;
        public int FavoriteCount { get; set; } = 0;
        public DateTime? LastViewedAt { get; set; }

        // === NAVIGATION PROPERTIES ===
        public ICollection<AssetTag> Tags { get; set; } = new List<AssetTag>();
        public ICollection<PlaylistAsset> PlaylistAssets { get; set; } = new List<PlaylistAsset>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<AssetAnalytics> AnalyticsEvents { get; set; } = new List<AssetAnalytics>();

        // === COMPUTED PROPERTIES ===
        public string ResolutionLabel => Height switch
        {
            >= 2160 => "4K",
            >= 1440 => "2K",
            >= 1080 => "1080p",
            >= 720 => "720p",
            >= 480 => "480p",
            _ => "Unknown"
        };

        public long EffectiveFileSizeBytes => CompressedFileSizeBytes ?? FileSizeBytes;
        public string EffectiveStoragePath => CompressedStoragePath ?? OriginalStoragePath;
    }
}
```

### 1.3 Supporting Entities

#### **User Entity**

```csharp
namespace MediaAssetManager.Core.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // OAuth Client Credentials (for API access)
        public string? ClientId { get; set; }
        public string? ClientSecretHash { get; set; }
        public DateTime? ClientSecretCreatedAt { get; set; }

        // Role-based permissions
        public UserRole Role { get; set; } = UserRole.User;

        // Navigation
        public ICollection<MediaAsset> Assets { get; set; } = new List<MediaAsset>();
        public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}
```

#### **AssetTag Entity** (Many-to-Many)

```csharp
namespace MediaAssetManager.Core.Entities
{
    public class AssetTag
    {
        public int AssetTagId { get; set; }
        public int AssetId { get; set; }
        public string Tag { get; set; } = string.Empty; // e.g., "pentakill", "ace", "funny"
        public DateTime CreatedAt { get; set; }

        // Navigation
        public MediaAsset Asset { get; set; } = null!;
    }
}
```

#### **Playlist Entity**

```csharp
namespace MediaAssetManager.Core.Entities
{
    public class Playlist
    {
        public int PlaylistId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<PlaylistAsset> PlaylistAssets { get; set; } = new List<PlaylistAsset>();
    }

    public class PlaylistAsset
    {
        public int PlaylistAssetId { get; set; }
        public int PlaylistId { get; set; }
        public int AssetId { get; set; }
        public int SortOrder { get; set; } // Order within playlist
        public DateTime AddedAt { get; set; }

        // Navigation
        public Playlist Playlist { get; set; } = null!;
        public MediaAsset Asset { get; set; } = null!;
    }
}
```

#### **Favorite Entity**

```csharp
namespace MediaAssetManager.Core.Entities
{
    public class Favorite
    {
        public int FavoriteId { get; set; }
        public int UserId { get; set; }
        public int AssetId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public MediaAsset Asset { get; set; } = null!;
    }
}
```

#### **AssetAnalytics Entity** (for tracking)

```csharp
namespace MediaAssetManager.Core.Entities
{
    public class AssetAnalytics
    {
        public long AnalyticsId { get; set; } // Use long for high volume
        public int AssetId { get; set; }
        public int? UserId { get; set; } // Null for anonymous views

        public AnalyticsEventType EventType { get; set; }
        public DateTime EventTimestamp { get; set; }

        // Additional context
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? ReferrerUrl { get; set; }

        // Navigation
        public MediaAsset Asset { get; set; } = null!;
    }
}
```

### 1.4 Enumerations

```csharp
namespace MediaAssetManager.Core.Enums
{
    public enum MediaType
    {
        Video = 1,
        Image = 2,
        Audio = 3
    }

    public enum ProcessingStatus
    {
        Uploaded = 1,           // Initial upload complete
        Queued = 2,             // Queued for processing
        ExtractingMetadata = 3, // Reading video properties
        GeneratingThumbnail = 4,
        Compressing = 5,        // Video compression in progress
        Ready = 6,              // All processing complete
        Failed = 7,             // Processing failed
        Deleted = 8             // Soft deleted
    }

    public enum UserRole
    {
        User = 1,
        Premium = 2,      // Higher upload limits, no compression
        Moderator = 3,
        Administrator = 4
    }

    public enum AnalyticsEventType
    {
        View = 1,
        Download = 2,
        Share = 3,
        Favorite = 4,
        Unfavorite = 5,
        AddedToPlaylist = 6
    }

    public enum VideoQuality
    {
        Original = 0,   // Keep original quality
        HD1080p = 1080, // 1920x1080
        HD720p = 720,   // 1280x720
        SD480p = 480    // 854x480
    }
}
```

### 1.5 Database Migration Strategy

```bash
# Add new migration
dotnet ef migrations add EnhancedMediaAssetModel --project MediaAssetManager.Infrastructure --startup-project MediaAssetManager.API

# Apply migration
dotnet ef database update --project MediaAssetManager.Infrastructure --startup-project MediaAssetManager.API
```

**Key Indexes** (add in `MediaAssetContext.OnModelCreating`):

```csharp
// Performance indexes
modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => a.UserId);
modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => a.GameTitle);
modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => a.Status);
modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => a.ContentHash); // Duplicate detection
modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => a.UploadedAt);
modelBuilder.Entity<MediaAsset>()
    .HasIndex(a => new { a.IsDeleted, a.IsPublic }); // Composite for queries

// Analytics partitioning-ready
modelBuilder.Entity<AssetAnalytics>()
    .HasIndex(a => new { a.AssetId, a.EventTimestamp });
modelBuilder.Entity<AssetAnalytics>()
    .HasIndex(a => new { a.EventType, a.EventTimestamp });
```

---

## 2. Video Processing Services

### 2.1 Architecture Overview

```
┌──────────────────────────────────────────────────────────┐
│              Video Processing Pipeline                    │
└──────────────────────────────────────────────────────────┘
                           │
                           ▼
        ┌──────────────────────────────────────┐
        │   1. Metadata Extraction Service     │
        │   - FFprobe for video analysis       │
        │   - Duration, codec, resolution, fps │
        └──────────────┬───────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────────────┐
        │   2. Thumbnail Generation Service    │
        │   - Extract frame at 3 seconds       │
        │   - Generate 3-second preview GIF    │
        └──────────────┬───────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────────────┐
        │   3. Video Compression Service       │
        │   - FFmpeg H.264 encoding            │
        │   - Target: 720p @ 2.5 Mbps         │
        │   - Optional based on original size  │
        └──────────────────────────────────────┘
```

### 2.2 Required NuGet Packages

```xml
<!-- MediaAssetManager.Services.csproj -->
<PackageReference Include="FFMpegCore" Version="5.1.0" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.9" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.8" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.2" />
```

**FFmpeg Installation**: Ensure FFmpeg binaries are installed on the server:

```bash
# Ubuntu/Debian
apt-get install ffmpeg

# Windows (use Chocolatey)
choco install ffmpeg

# Docker
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y ffmpeg
```

### 2.3 IVideoMetadataService

**File**: `MediaAssetManager.Services/Interfaces/IVideoMetadataService.cs`

```csharp
namespace MediaAssetManager.Services.Interfaces
{
    public interface IVideoMetadataService
    {
        /// <summary>
        /// Extract video metadata using FFprobe
        /// </summary>
        Task<VideoMetadata> ExtractMetadataAsync(string filePath);

        /// <summary>
        /// Extract metadata from stream (for in-memory processing)
        /// </summary>
        Task<VideoMetadata> ExtractMetadataAsync(Stream videoStream, string tempFileName);

        /// <summary>
        /// Validate if file is a supported video format
        /// </summary>
        Task<bool> IsValidVideoAsync(string filePath);
    }

    public record VideoMetadata
    {
        public int DurationSeconds { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public string Codec { get; init; } = string.Empty;
        public int BitrateKbps { get; init; }
        public decimal FrameRate { get; init; }
        public string AudioCodec { get; init; } = string.Empty;
        public string Format { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
    }
}
```

### 2.4 VideoMetadataService Implementation

**File**: `MediaAssetManager.Services/VideoMetadataService.cs`

```csharp
using FFMpegCore;
using FFMpegCore.Enums;
using MediaAssetManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MediaAssetManager.Services
{
    public class VideoMetadataService : IVideoMetadataService
    {
        private readonly ILogger<VideoMetadataService> _logger;

        // Supported video formats
        private static readonly string[] SupportedFormats =
        {
            ".mp4", ".mov", ".avi", ".mkv", ".webm", ".flv", ".wmv", ".m4v"
        };

        public VideoMetadataService(ILogger<VideoMetadataService> logger)
        {
            _logger = logger;

            // Configure FFmpeg binary path if needed
            // GlobalFFOptions.Configure(new FFOptions { BinaryFolder = "/usr/bin" });
        }

        public async Task<VideoMetadata> ExtractMetadataAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Extracting metadata from {FilePath}", filePath);

                var mediaInfo = await FFProbe.AnalyseAsync(filePath);

                if (mediaInfo.VideoStreams.Count == 0)
                {
                    throw new InvalidOperationException("No video stream found in file");
                }

                var videoStream = mediaInfo.VideoStreams.First();
                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

                var metadata = new VideoMetadata
                {
                    DurationSeconds = (int)mediaInfo.Duration.TotalSeconds,
                    Width = videoStream.Width,
                    Height = videoStream.Height,
                    Codec = videoStream.CodecName,
                    BitrateKbps = (int)(mediaInfo.BitRate / 1000),
                    FrameRate = (decimal)videoStream.FrameRate,
                    AudioCodec = audioStream?.CodecName ?? "none",
                    Format = mediaInfo.Format.FormatName,
                    FileSizeBytes = new FileInfo(filePath).Length
                };

                _logger.LogInformation(
                    "Metadata extracted: {Duration}s, {Width}x{Height}, {Codec}, {Bitrate}kbps, {FPS}fps",
                    metadata.DurationSeconds, metadata.Width, metadata.Height,
                    metadata.Codec, metadata.BitrateKbps, metadata.FrameRate
                );

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract metadata from {FilePath}", filePath);
                throw;
            }
        }

        public async Task<VideoMetadata> ExtractMetadataAsync(Stream videoStream, string tempFileName)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

            try
            {
                // Save stream to temp file
                await using (var fileStream = File.Create(tempPath))
                {
                    await videoStream.CopyToAsync(fileStream);
                }

                return await ExtractMetadataAsync(tempPath);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public async Task<bool> IsValidVideoAsync(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (!SupportedFormats.Contains(extension))
                {
                    return false;
                }

                var mediaInfo = await FFProbe.AnalyseAsync(filePath);
                return mediaInfo.VideoStreams.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
```

### 2.5 IThumbnailService

**File**: `MediaAssetManager.Services/Interfaces/IThumbnailService.cs`

```csharp
namespace MediaAssetManager.Services.Interfaces
{
    public interface IThumbnailService
    {
        /// <summary>
        /// Generate thumbnail from video at specified timestamp
        /// </summary>
        Task<Stream> GenerateThumbnailAsync(
            string videoPath,
            int timestampSeconds = 3,
            int width = 1280,
            int height = 720
        );

        /// <summary>
        /// Generate animated preview GIF (first 3 seconds)
        /// </summary>
        Task<Stream> GeneratePreviewGifAsync(
            string videoPath,
            int durationSeconds = 3,
            int width = 480,
            int fps = 10
        );
    }
}
```

### 2.6 ThumbnailService Implementation

```csharp
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using MediaAssetManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MediaAssetManager.Services
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;

        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
        }

        public async Task<Stream> GenerateThumbnailAsync(
            string videoPath,
            int timestampSeconds = 3,
            int width = 1280,
            int height = 720)
        {
            try
            {
                _logger.LogInformation(
                    "Generating thumbnail for {VideoPath} at {Timestamp}s",
                    videoPath, timestampSeconds
                );

                var outputPath = Path.Combine(
                    Path.GetTempPath(),
                    $"thumb_{Guid.NewGuid()}.jpg"
                );

                await FFMpeg.SnapshotAsync(
                    videoPath,
                    outputPath,
                    new Size(width, height),
                    TimeSpan.FromSeconds(timestampSeconds)
                );

                var stream = new MemoryStream();
                await using (var fileStream = File.OpenRead(outputPath))
                {
                    await fileStream.CopyToAsync(stream);
                }

                stream.Position = 0;

                // Cleanup temp file
                File.Delete(outputPath);

                _logger.LogInformation("Thumbnail generated successfully");
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate thumbnail for {VideoPath}", videoPath);
                throw;
            }
        }

        public async Task<Stream> GeneratePreviewGifAsync(
            string videoPath,
            int durationSeconds = 3,
            int width = 480,
            int fps = 10)
        {
            try
            {
                _logger.LogInformation(
                    "Generating preview GIF for {VideoPath} ({Duration}s)",
                    videoPath, durationSeconds
                );

                var outputPath = Path.Combine(
                    Path.GetTempPath(),
                    $"preview_{Guid.NewGuid()}.gif"
                );

                await FFMpeg.GifSnapshotAsync(
                    videoPath,
                    outputPath,
                    new Size(width, -1), // Maintain aspect ratio
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(durationSeconds),
                    fps
                );

                var stream = new MemoryStream();
                await using (var fileStream = File.OpenRead(outputPath))
                {
                    await fileStream.CopyToAsync(stream);
                }

                stream.Position = 0;

                File.Delete(outputPath);

                _logger.LogInformation("Preview GIF generated successfully");
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate preview GIF for {VideoPath}", videoPath);
                throw;
            }
        }
    }
}
```

### 2.7 IVideoCompressionService

**File**: `MediaAssetManager.Services/Interfaces/IVideoCompressionService.cs`

```csharp
namespace MediaAssetManager.Services.Interfaces
{
    public interface IVideoCompressionService
    {
        /// <summary>
        /// Compress video to target quality with H.264 codec
        /// </summary>
        Task<CompressionResult> CompressVideoAsync(
            string inputPath,
            string outputPath,
            VideoQuality targetQuality = VideoQuality.HD720p,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Determine if compression is beneficial (based on file size/quality)
        /// </summary>
        bool ShouldCompress(VideoMetadata metadata, VideoQuality targetQuality);
    }

    public record CompressionResult
    {
        public bool Success { get; init; }
        public long OriginalSizeBytes { get; init; }
        public long CompressedSizeBytes { get; init; }
        public decimal CompressionRatio { get; init; }
        public string OutputPath { get; init; } = string.Empty;
        public string? ErrorMessage { get; init; }
    }
}
```

### 2.8 VideoCompressionService Implementation

```csharp
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using MediaAssetManager.Core.Enums;
using MediaAssetManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MediaAssetManager.Services
{
    public class VideoCompressionService : IVideoCompressionService
    {
        private readonly ILogger<VideoCompressionService> _logger;

        // Quality presets: resolution -> bitrate (kbps)
        private static readonly Dictionary<VideoQuality, int> BitrateTargets = new()
        {
            { VideoQuality.HD1080p, 5000 },
            { VideoQuality.HD720p, 2500 },
            { VideoQuality.SD480p, 1000 }
        };

        public VideoCompressionService(ILogger<VideoCompressionService> logger)
        {
            _logger = logger;
        }

        public async Task<CompressionResult> CompressVideoAsync(
            string inputPath,
            string outputPath,
            VideoQuality targetQuality = VideoQuality.HD720p,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var originalSize = new FileInfo(inputPath).Length;

                _logger.LogInformation(
                    "Compressing {InputPath} to {Quality} quality",
                    inputPath, targetQuality
                );

                var targetBitrate = BitrateTargets[targetQuality];
                var targetHeight = (int)targetQuality;

                var success = await FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, overwrite: true, options => options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithConstantRateFactor(23) // Quality factor (18-28, lower = better)
                        .WithVideoBitrate(targetBitrate)
                        .WithVideoFilters(filterOptions => filterOptions
                            .Scale(height: targetHeight) // Scale maintaining aspect ratio
                        )
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithAudioBitrate(128)
                        .WithFastStart() // Enable streaming
                    )
                    .CancellableThrough(cancellationToken)
                    .ProcessAsynchronously();

                if (!success)
                {
                    return new CompressionResult
                    {
                        Success = false,
                        ErrorMessage = "FFmpeg compression failed"
                    };
                }

                var compressedSize = new FileInfo(outputPath).Length;
                var ratio = (decimal)compressedSize / originalSize;

                _logger.LogInformation(
                    "Compression complete: {OriginalMB:F2} MB -> {CompressedMB:F2} MB ({Ratio:P0} of original)",
                    originalSize / 1024.0 / 1024.0,
                    compressedSize / 1024.0 / 1024.0,
                    ratio
                );

                return new CompressionResult
                {
                    Success = true,
                    OriginalSizeBytes = originalSize,
                    CompressedSizeBytes = compressedSize,
                    CompressionRatio = ratio,
                    OutputPath = outputPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compression failed for {InputPath}", inputPath);
                return new CompressionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public bool ShouldCompress(VideoMetadata metadata, VideoQuality targetQuality)
        {
            // Don't compress if already at or below target quality
            if (metadata.Height <= (int)targetQuality)
            {
                return false;
            }

            // Don't compress if already efficiently encoded
            var targetBitrate = BitrateTargets[targetQuality];
            if (metadata.BitrateKbps <= targetBitrate * 1.2) // 20% tolerance
            {
                return false;
            }

            // Compress if file is large
            var fileSizeMB = metadata.FileSizeBytes / 1024.0 / 1024.0;
            return fileSizeMB > 50; // Compress anything over 50MB
        }
    }
}
```

---

## 3. Background Orchestration & Transactions

### 3.1 Hangfire Setup

**Install Hangfire**:

```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql
```

**Configure in Program.cs**:

```csharp
// Add Hangfire services
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "video-processing", "thumbnails", "default" };
});

// After app.Build()
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

### 3.2 Video Processing Orchestrator

**File**: `MediaAssetManager.Services/Orchestrators/VideoProcessingOrchestrator.cs`

```csharp
using Hangfire;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Enums;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MediaAssetManager.Services.Orchestrators
{
    public interface IVideoProcessingOrchestrator
    {
        /// <summary>
        /// Queue a video for background processing
        /// </summary>
        string EnqueueVideoProcessing(int assetId);

        /// <summary>
        /// Synchronously process video (for small files)
        /// </summary>
        Task ProcessVideoAsync(int assetId, CancellationToken cancellationToken = default);
    }

    public class VideoProcessingOrchestrator : IVideoProcessingOrchestrator
    {
        private readonly IMediaAssetRepository _assetRepository;
        private readonly IStorageService _storageService;
        private readonly IVideoMetadataService _metadataService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IVideoCompressionService _compressionService;
        private readonly ILogger<VideoProcessingOrchestrator> _logger;
        private readonly IBackgroundJobClient _backgroundJobs;

        public VideoProcessingOrchestrator(
            IMediaAssetRepository assetRepository,
            IStorageService storageService,
            IVideoMetadataService metadataService,
            IThumbnailService thumbnailService,
            IVideoCompressionService compressionService,
            ILogger<VideoProcessingOrchestrator> logger,
            IBackgroundJobClient backgroundJobs)
        {
            _assetRepository = assetRepository;
            _storageService = storageService;
            _metadataService = metadataService;
            _thumbnailService = thumbnailService;
            _compressionService = compressionService;
            _logger = logger;
            _backgroundJobs = backgroundJobs;
        }

        public string EnqueueVideoProcessing(int assetId)
        {
            var jobId = _backgroundJobs.Enqueue<VideoProcessingOrchestrator>(
                x => x.ProcessVideoAsync(assetId, CancellationToken.None)
            );

            _logger.LogInformation("Enqueued video processing job {JobId} for asset {AssetId}", jobId, assetId);
            return jobId;
        }

        [Queue("video-processing")]
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessVideoAsync(int assetId, CancellationToken cancellationToken = default)
        {
            var tempFiles = new List<string>();

            try
            {
                _logger.LogInformation("Starting video processing for asset {AssetId}", assetId);

                // 1. Load asset
                var asset = await _assetRepository.GetByIdAsync(assetId)
                    ?? throw new InvalidOperationException($"Asset {assetId} not found");

                await UpdateStatus(asset, ProcessingStatus.ExtractingMetadata);

                // 2. Download original from B2 to temp location
                var tempVideoPath = Path.Combine(Path.GetTempPath(), $"{assetId}_{asset.FileName}");
                tempFiles.Add(tempVideoPath);

                await DownloadFromB2(asset.OriginalStoragePath, tempVideoPath);

                // 3. Extract metadata
                var metadata = await _metadataService.ExtractMetadataAsync(tempVideoPath);
                asset.DurationSeconds = metadata.DurationSeconds;
                asset.Width = metadata.Width;
                asset.Height = metadata.Height;
                asset.Codec = metadata.Codec;
                asset.BitrateKbps = metadata.BitrateKbps;
                asset.FrameRate = metadata.FrameRate;
                asset.AudioCodec = metadata.AudioCodec;

                await _assetRepository.UpdateAsync(asset);

                // 4. Generate thumbnail
                await UpdateStatus(asset, ProcessingStatus.GeneratingThumbnail);
                await GenerateAndUploadThumbnail(asset, tempVideoPath, tempFiles);

                // 5. Compression (optional based on size)
                if (_compressionService.ShouldCompress(metadata, VideoQuality.HD720p))
                {
                    await UpdateStatus(asset, ProcessingStatus.Compressing);
                    await CompressAndUploadVideo(asset, tempVideoPath, tempFiles, cancellationToken);
                }

                // 6. Mark as ready
                await UpdateStatus(asset, ProcessingStatus.Ready);
                asset.ProcessedAt = DateTime.UtcNow;
                await _assetRepository.UpdateAsync(asset);

                _logger.LogInformation("Video processing completed for asset {AssetId}", assetId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video processing failed for asset {AssetId}", assetId);

                var asset = await _assetRepository.GetByIdAsync(assetId);
                if (asset != null)
                {
                    asset.Status = ProcessingStatus.Failed;
                    asset.ProcessingError = ex.Message;
                    await _assetRepository.UpdateAsync(asset);
                }

                throw; // Re-throw for Hangfire retry
            }
            finally
            {
                // Cleanup temp files
                foreach (var file in tempFiles.Where(File.Exists))
                {
                    try { File.Delete(file); } catch { /* Ignore cleanup errors */ }
                }
            }
        }

        private async Task UpdateStatus(MediaAsset asset, ProcessingStatus status)
        {
            asset.Status = status;
            await _assetRepository.UpdateAsync(asset);
            _logger.LogInformation("Asset {AssetId} status: {Status}", asset.AssetId, status);
        }

        private async Task DownloadFromB2(string storagePath, string localPath)
        {
            var signedUrl = await _storageService.GetSignedDownloadUrlAsync(storagePath);

            using var httpClient = new HttpClient();
            await using var stream = await httpClient.GetStreamAsync(signedUrl);
            await using var fileStream = File.Create(localPath);
            await stream.CopyToAsync(fileStream);
        }

        private async Task GenerateAndUploadThumbnail(MediaAsset asset, string videoPath, List<string> tempFiles)
        {
            // Generate thumbnail
            await using var thumbnailStream = await _thumbnailService.GenerateThumbnailAsync(videoPath);

            // Upload to B2
            var thumbnailPath = $"thumbnails/{asset.AssetId}_{Guid.NewGuid()}.jpg";
            var (_, fileName, _) = await _storageService.UploadFileAsync(
                thumbnailStream,
                thumbnailPath,
                "image/jpeg"
            );

            asset.ThumbnailStoragePath = fileName;

            // Optionally generate preview GIF
            if (asset.DurationSeconds > 3)
            {
                await using var gifStream = await _thumbnailService.GeneratePreviewGifAsync(videoPath);
                var gifPath = $"previews/{asset.AssetId}_{Guid.NewGuid()}.gif";
                var (_, gifFileName, _) = await _storageService.UploadFileAsync(
                    gifStream,
                    gifPath,
                    "image/gif"
                );
                asset.PreviewGifStoragePath = gifFileName;
            }

            await _assetRepository.UpdateAsync(asset);
        }

        private async Task CompressAndUploadVideo(
            MediaAsset asset,
            string inputPath,
            List<string> tempFiles,
            CancellationToken cancellationToken)
        {
            var compressedPath = Path.Combine(
                Path.GetTempPath(),
                $"{asset.AssetId}_compressed.mp4"
            );
            tempFiles.Add(compressedPath);

            var result = await _compressionService.CompressVideoAsync(
                inputPath,
                compressedPath,
                VideoQuality.HD720p,
                cancellationToken
            );

            if (result.Success)
            {
                // Upload compressed version
                await using var compressedStream = File.OpenRead(compressedPath);
                var storagePath = $"compressed/{asset.AssetId}_{Guid.NewGuid()}.mp4";

                var (_, fileName, fileSize) = await _storageService.UploadFileAsync(
                    compressedStream,
                    storagePath,
                    "video/mp4"
                );

                asset.CompressedStoragePath = fileName;
                asset.CompressedFileSizeBytes = fileSize;
                asset.CompressionRatio = result.CompressionRatio;

                await _assetRepository.UpdateAsync(asset);

                _logger.LogInformation(
                    "Compressed video uploaded: {OriginalMB:F2} MB -> {CompressedMB:F2} MB",
                    result.OriginalSizeBytes / 1024.0 / 1024.0,
                    result.CompressedSizeBytes / 1024.0 / 1024.0
                );
            }
            else
            {
                _logger.LogWarning("Compression skipped or failed: {Error}", result.ErrorMessage);
            }
        }
    }
}
```

### 3.3 Transaction Management with Unit of Work Pattern

**File**: `MediaAssetManager.Core/Interfaces/IUnitOfWork.cs`

```csharp
namespace MediaAssetManager.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IMediaAssetRepository MediaAssets { get; }
        // Add other repositories as needed

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
```

**Implementation**:

```csharp
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace MediaAssetManager.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MediaAssetContext _context;
        private IDbContextTransaction? _transaction;
        private IMediaAssetRepository? _mediaAssets;

        public UnitOfWork(MediaAssetContext context)
        {
            _context = context;
        }

        public IMediaAssetRepository MediaAssets =>
            _mediaAssets ??= new MediaAssetRepository(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                await _transaction!.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
```

---

## 4. API Extensions

### 4.1 Upload Endpoint (Multipart Form Data)

**File**: `MediaAssetManager.API/Controllers/MediaAssetsController.cs`

```csharp
[HttpPost("upload")]
[RequestSizeLimit(2_147_483_648)] // 2GB max
[DisableRequestSizeLimit] // For Kestrel
public async Task<ActionResult<MediaAssetResponse>> UploadVideo(
    [FromForm] UploadVideoRequest request,
    IFormFile file)
{
    try
    {
        // 1. Validation
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (file.Length > 2_147_483_648) // 2GB
            return BadRequest(new { error = "File too large (max 2GB)" });

        var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Unsupported video format" });

        // 2. Calculate content hash (for duplicate detection)
        string contentHash;
        await using (var hashStream = file.OpenReadStream())
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(hashStream);
            contentHash = Convert.ToHexString(hashBytes);
        }

        // Check for duplicate
        var existingAsset = await _assetService.FindByContentHashAsync(contentHash);
        if (existingAsset != null)
        {
            return Ok(new {
                message = "Duplicate detected",
                existingAssetId = existingAsset.AssetId
            });
        }

        // 3. Upload to B2
        var userId = GetCurrentUserId(); // From JWT claims
        var storagePath = $"originals/{userId}/{Guid.NewGuid()}{extension}";

        await using var uploadStream = file.OpenReadStream();
        var (fileId, fileName, fileSize) = await _storageService.UploadFileAsync(
            uploadStream,
            storagePath,
            file.ContentType
        );

        // 4. Create database record
        var asset = new MediaAsset
        {
            UserId = userId,
            FileName = fileName,
            OriginalFileName = file.FileName,
            FileSizeBytes = fileSize,
            Title = request.Title,
            Description = request.Description,
            GameTitle = request.GameTitle,
            ContentHash = contentHash,
            OriginalStoragePath = fileName,
            Status = ProcessingStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
            MediaType = MediaType.Video,
            IsPublic = request.IsPublic ?? true
        };

        var createdAsset = await _assetService.CreateAsync(asset);

        // 5. Queue background processing
        var shouldProcessAsync = fileSize > 50_000_000; // 50MB threshold
        if (shouldProcessAsync)
        {
            var jobId = _videoOrchestrator.EnqueueVideoProcessing(createdAsset.AssetId);
            _logger.LogInformation("Queued processing job {JobId} for asset {AssetId}", jobId, createdAsset.AssetId);
        }
        else
        {
            // Process synchronously for small files
            await _videoOrchestrator.ProcessVideoAsync(createdAsset.AssetId);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdAsset.AssetId },
            createdAsset.ToResponse()
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Upload failed");
        return StatusCode(500, new { error = "Upload failed" });
    }
}
```

### 4.2 Stream/Download Endpoint (with Redirect)

```csharp
[HttpGet("{id}/stream")]
public async Task<IActionResult> StreamVideo(int id, [FromQuery] bool download = false)
{
    var asset = await _assetService.GetByIdAsync(id);
    if (asset == null) return NotFound();

    // Check permissions (if private video)
    if (!asset.IsPublic && !await _authService.CanAccessAsset(GetCurrentUserId(), id))
    {
        return Forbid();
    }

    // Track analytics
    await _analyticsService.TrackEventAsync(new AssetAnalytics
    {
        AssetId = id,
        UserId = GetCurrentUserId(),
        EventType = download ? AnalyticsEventType.Download : AnalyticsEventType.View,
        EventTimestamp = DateTime.UtcNow,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = Request.Headers.UserAgent.ToString()
    });

    // Increment view count
    await _assetService.IncrementViewCountAsync(id);

    // Generate signed URL (valid for 1 hour)
    var signedUrl = await _storageService.GetSignedDownloadUrlAsync(
        asset.EffectiveStoragePath,
        expirationSeconds: 3600
    );

    // Redirect to B2 signed URL (clean URL for user, tracking on our side)
    return Redirect(signedUrl);
}
```

### 4.3 Delete Endpoint (with Cascade)

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteVideo(int id)
{
    var asset = await _assetService.GetByIdAsync(id);
    if (asset == null) return NotFound();

    // Check ownership
    if (asset.UserId != GetCurrentUserId() && !await _authService.IsAdmin(GetCurrentUserId()))
    {
        return Forbid();
    }

    try
    {
        // Use transaction for cascade delete
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        // 1. Delete from B2
        var filesToDelete = new List<string> { asset.OriginalStoragePath };
        if (asset.CompressedStoragePath != null)
            filesToDelete.Add(asset.CompressedStoragePath);
        if (asset.ThumbnailStoragePath != null)
            filesToDelete.Add(asset.ThumbnailStoragePath);
        if (asset.PreviewGifStoragePath != null)
            filesToDelete.Add(asset.PreviewGifStoragePath);

        foreach (var file in filesToDelete)
        {
            await _storageService.DeleteFileAsync(null, file);
        }

        // 2. Soft delete in database (preserve analytics)
        asset.IsDeleted = true;
        asset.DeletedAt = DateTime.UtcNow;
        asset.Status = ProcessingStatus.Deleted;
        await _assetService.UpdateAsync(asset);

        await _unitOfWork.CommitTransactionAsync();

        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete asset {AssetId}", id);
        await _unitOfWork.RollbackTransactionAsync();
        return StatusCode(500, new { error = "Delete failed" });
    }
}
```

### 4.4 Additional Endpoints

```csharp
// Get thumbnail
[HttpGet("{id}/thumbnail")]
public async Task<IActionResult> GetThumbnail(int id)
{
    var asset = await _assetService.GetByIdAsync(id);
    if (asset?.ThumbnailStoragePath == null) return NotFound();

    var url = await _storageService.GetSignedDownloadUrlAsync(asset.ThumbnailStoragePath);
    return Redirect(url);
}

// Search by game
[HttpGet("games/{gameTitle}")]
public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> GetByGame(
    string gameTitle,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var query = new MediaAssetQuery
    {
        GameTitle = gameTitle,
        PageNumber = page,
        PageSize = pageSize,
        SortBy = MediaAssetSortBy.UploadedAt,
        SortDescending = true
    };

    var result = await _assetService.GetAssetsAsync(query);
    return Ok(result.ToPaginatedResponse());
}

// Get user's clips
[HttpGet("my-clips")]
[Authorize]
public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> GetMyClips(
    [FromQuery] MediaAssetQueryRequest request)
{
    var query = request.ToQuery();
    query.UserId = GetCurrentUserId();

    var result = await _assetService.GetAssetsAsync(query);
    return Ok(result.ToPaginatedResponse());
}
```

---

## 5. B2 Bucket Organization

### 5.1 Folder Structure

```
media-asset-manager-storage-dev/
│
├── originals/
│   ├── {userId}/
│   │   ├── {guid}.mp4
│   │   ├── {guid}.mov
│   │   └── ...
│   └── ...
│
├── compressed/
│   ├── {assetId}_{guid}.mp4
│   └── ...
│
├── thumbnails/
│   ├── {assetId}_{guid}.jpg
│   └── ...
│
├── previews/
│   ├── {assetId}_{guid}.gif
│   └── ...
│
└── temp/
    └── {upload-session-id}/
        └── {chunk-files}
```

### 5.2 Enhanced Storage Service

```csharp
public class B2StorageService : IStorageService
{
    // Folder constants
    private const string ORIGINALS_FOLDER = "originals";
    private const string COMPRESSED_FOLDER = "compressed";
    private const string THUMBNAILS_FOLDER = "thumbnails";
    private const string PREVIEWS_FOLDER = "previews";

    public async Task<(string FileId, string FileName, long FileSize)> UploadOriginalAsync(
        Stream fileStream,
        int userId,
        string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var key = $"{ORIGINALS_FOLDER}/{userId}/{Guid.NewGuid()}{extension}";

        return await UploadFileAsync(fileStream, key, GetContentType(extension));
    }

    public async Task<(string FileId, string FileName, long FileSize)> UploadCompressedAsync(
        Stream fileStream,
        int assetId)
    {
        var key = $"{COMPRESSED_FOLDER}/{assetId}_{Guid.NewGuid()}.mp4";
        return await UploadFileAsync(fileStream, key, "video/mp4");
    }

    public async Task<(string FileId, string FileName, long FileSize)> UploadThumbnailAsync(
        Stream imageStream,
        int assetId)
    {
        var key = $"{THUMBNAILS_FOLDER}/{assetId}_{Guid.NewGuid()}.jpg";
        return await UploadFileAsync(imageStream, key, "image/jpeg");
    }

    public async Task<(string FileId, string FileName, long FileSize)> UploadPreviewGifAsync(
        Stream gifStream,
        int assetId)
    {
        var key = $"{PREVIEWS_FOLDER}/{assetId}_{Guid.NewGuid()}.gif";
        return await UploadFileAsync(fileStream, key, "image/gif");
    }

    // Multipart upload for large files (>100MB)
    public async Task<string> InitiateMultipartUploadAsync(string key)
    {
        var request = new InitiateMultipartUploadRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        var response = await _s3Client.InitiateMultipartUploadAsync(request);
        return response.UploadId;
    }

    public async Task<string> UploadPartAsync(
        string key,
        string uploadId,
        int partNumber,
        Stream partStream)
    {
        var request = new UploadPartRequest
        {
            BucketName = _bucketName,
            Key = key,
            UploadId = uploadId,
            PartNumber = partNumber,
            InputStream = partStream
        };

        var response = await _s3Client.UploadPartAsync(request);
        return response.ETag;
    }

    public async Task CompleteMultipartUploadAsync(
        string key,
        string uploadId,
        List<PartETag> parts)
    {
        var request = new CompleteMultipartUploadRequest
        {
            BucketName = _bucketName,
            Key = key,
            UploadId = uploadId,
            PartETags = parts
        };

        await _s3Client.CompleteMultipartUploadAsync(request);
    }

    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }
}
```

### 5.3 Lifecycle Rules (B2 Configuration)

Configure in Backblaze B2 console:

```json
{
	"fileLifecycleRules": [
		{
			"daysFromUploadingToHiding": null,
			"daysFromHidingToDeleting": 30,
			"fileNamePrefix": "temp/"
		},
		{
			"daysFromUploadingToHiding": 90,
			"daysFromHidingToDeleting": 30,
			"fileNamePrefix": "originals/",
			"note": "Archive originals after compression"
		}
	]
}
```

---

## 6. Advanced Query Object Extensions

### 6.1 Enhanced MediaAssetQuery

**File**: `MediaAssetManager.Core/Queries/MediaAssetQuery.cs`

```csharp
namespace MediaAssetManager.Core.Queries
{
    public class MediaAssetQuery
    {
        // === EXISTING FILTERS ===
        public string? FileName { get; set; }
        public string? Title { get; set; }
        public long? MinFileSizeBytes { get; set; }
        public long? MaxFileSizeBytes { get; set; }
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }

        // === NEW: USER & OWNERSHIP ===
        public int? UserId { get; set; }
        public bool? IsPublic { get; set; }
        public bool IncludeDeleted { get; set; } = false;

        // === NEW: GAMING CONTEXT ===
        public string? GameTitle { get; set; }
        public string? SessionId { get; set; }
        public List<string>? Tags { get; set; } // Search by any tag
        public bool? HasTags { get; set; } // Filter clips with/without tags

        // === NEW: VIDEO PROPERTIES ===
        public int? MinDurationSeconds { get; set; }
        public int? MaxDurationSeconds { get; set; }
        public int? MinWidth { get; set; }  // e.g., 1280 for HD+
        public int? MinHeight { get; set; } // e.g., 720 for HD
        public List<string>? Codecs { get; set; } // Filter by codec
        public decimal? MinFrameRate { get; set; } // e.g., 60.0 for high FPS

        // === NEW: PROCESSING STATUS ===
        public ProcessingStatus? Status { get; set; }
        public bool? IsCompressed { get; set; }
        public bool? HasThumbnail { get; set; }

        // === NEW: ANALYTICS ===
        public int? MinViewCount { get; set; }
        public int? MinFavoriteCount { get; set; }
        public DateTime? ViewedAfter { get; set; } // Recently watched

        // === NEW: FULL-TEXT SEARCH ===
        public string? SearchQuery { get; set; } // Search across title, description, tags

        // === SORTING ===
        public MediaAssetSortBy SortBy { get; set; } = MediaAssetSortBy.UploadedAt;
        public bool SortDescending { get; set; } = true;

        // === PAGINATION ===
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
```

### 6.2 Extended Sort Options

```csharp
namespace MediaAssetManager.Core.Queries
{
    public enum MediaAssetSortBy
    {
        UploadedAt = 1,
        FileName = 2,
        FileSizeBytes = 3,
        Title = 4,

        // NEW
        DurationSeconds = 5,
        ViewCount = 6,
        FavoriteCount = 7,
        GameTitle = 8,
        Resolution = 9,        // Sort by Height
        LastViewedAt = 10,
        Relevance = 11         // For search query ranking
    }
}
```

### 6.3 Query Extension Methods

**File**: `MediaAssetManager.Infrastructure/Extensions/MediaAssetQueryExtensions.cs`

```csharp
public static class MediaAssetQueryExtensions
{
    public static IQueryable<MediaAsset> ApplyFilters(
        this IQueryable<MediaAsset> query,
        MediaAssetQuery filter)
    {
        // Existing filters
        if (!string.IsNullOrWhiteSpace(filter.FileName))
            query = query.Where(a => a.FileName.Contains(filter.FileName));

        if (!string.IsNullOrWhiteSpace(filter.Title))
            query = query.Where(a => a.Title != null && a.Title.Contains(filter.Title));

        if (filter.MinFileSizeBytes.HasValue)
            query = query.Where(a => a.FileSizeBytes >= filter.MinFileSizeBytes.Value);

        if (filter.MaxFileSizeBytes.HasValue)
            query = query.Where(a => a.FileSizeBytes <= filter.MaxFileSizeBytes.Value);

        if (filter.UploadedAfter.HasValue)
            query = query.Where(a => a.UploadedAt >= filter.UploadedAfter.Value);

        if (filter.UploadedBefore.HasValue)
            query = query.Where(a => a.UploadedAt <= filter.UploadedBefore.Value);

        // === NEW FILTERS ===

        // User & ownership
        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId.Value);

        if (filter.IsPublic.HasValue)
            query = query.Where(a => a.IsPublic == filter.IsPublic.Value);

        if (!filter.IncludeDeleted)
            query = query.Where(a => !a.IsDeleted);

        // Gaming context
        if (!string.IsNullOrWhiteSpace(filter.GameTitle))
            query = query.Where(a => a.GameTitle != null &&
                                      a.GameTitle.Contains(filter.GameTitle));

        if (!string.IsNullOrWhiteSpace(filter.SessionId))
            query = query.Where(a => a.SessionId == filter.SessionId);

        // Tags (any match)
        if (filter.Tags != null && filter.Tags.Any())
        {
            query = query.Where(a => a.Tags.Any(t => filter.Tags.Contains(t.Tag)));
        }

        if (filter.HasTags.HasValue)
        {
            query = filter.HasTags.Value
                ? query.Where(a => a.Tags.Any())
                : query.Where(a => !a.Tags.Any());
        }

        // Video properties
        if (filter.MinDurationSeconds.HasValue)
            query = query.Where(a => a.DurationSeconds >= filter.MinDurationSeconds.Value);

        if (filter.MaxDurationSeconds.HasValue)
            query = query.Where(a => a.DurationSeconds <= filter.MaxDurationSeconds.Value);

        if (filter.MinWidth.HasValue)
            query = query.Where(a => a.Width >= filter.MinWidth.Value);

        if (filter.MinHeight.HasValue)
            query = query.Where(a => a.Height >= filter.MinHeight.Value);

        if (filter.Codecs != null && filter.Codecs.Any())
            query = query.Where(a => a.Codec != null && filter.Codecs.Contains(a.Codec));

        if (filter.MinFrameRate.HasValue)
            query = query.Where(a => a.FrameRate >= filter.MinFrameRate.Value);

        // Processing status
        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);

        if (filter.IsCompressed.HasValue)
        {
            query = filter.IsCompressed.Value
                ? query.Where(a => a.CompressedStoragePath != null)
                : query.Where(a => a.CompressedStoragePath == null);
        }

        if (filter.HasThumbnail.HasValue)
        {
            query = filter.HasThumbnail.Value
                ? query.Where(a => a.ThumbnailStoragePath != null)
                : query.Where(a => a.ThumbnailStoragePath == null);
        }

        // Analytics
        if (filter.MinViewCount.HasValue)
            query = query.Where(a => a.ViewCount >= filter.MinViewCount.Value);

        if (filter.MinFavoriteCount.HasValue)
            query = query.Where(a => a.FavoriteCount >= filter.MinFavoriteCount.Value);

        if (filter.ViewedAfter.HasValue)
            query = query.Where(a => a.LastViewedAt >= filter.ViewedAfter.Value);

        // Full-text search (PostgreSQL)
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var searchTerm = filter.SearchQuery.ToLower();
            query = query.Where(a =>
                (a.Title != null && a.Title.ToLower().Contains(searchTerm)) ||
                (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                (a.GameTitle != null && a.GameTitle.ToLower().Contains(searchTerm)) ||
                a.Tags.Any(t => t.Tag.ToLower().Contains(searchTerm))
            );
        }

        return query;
    }

    public static IQueryable<MediaAsset> ApplySorting(
        this IQueryable<MediaAsset> query,
        MediaAssetQuery filter)
    {
        query = filter.SortBy switch
        {
            MediaAssetSortBy.UploadedAt => filter.SortDescending
                ? query.OrderByDescending(a => a.UploadedAt)
                : query.OrderBy(a => a.UploadedAt),

            MediaAssetSortBy.FileName => filter.SortDescending
                ? query.OrderByDescending(a => a.FileName)
                : query.OrderBy(a => a.FileName),

            MediaAssetSortBy.FileSizeBytes => filter.SortDescending
                ? query.OrderByDescending(a => a.FileSizeBytes)
                : query.OrderBy(a => a.FileSizeBytes),

            MediaAssetSortBy.Title => filter.SortDescending
                ? query.OrderByDescending(a => a.Title)
                : query.OrderBy(a => a.Title),

            // NEW
            MediaAssetSortBy.DurationSeconds => filter.SortDescending
                ? query.OrderByDescending(a => a.DurationSeconds)
                : query.OrderBy(a => a.DurationSeconds),

            MediaAssetSortBy.ViewCount => filter.SortDescending
                ? query.OrderByDescending(a => a.ViewCount)
                : query.OrderBy(a => a.ViewCount),

            MediaAssetSortBy.FavoriteCount => filter.SortDescending
                ? query.OrderByDescending(a => a.FavoriteCount)
                : query.OrderBy(a => a.FavoriteCount),

            MediaAssetSortBy.GameTitle => filter.SortDescending
                ? query.OrderByDescending(a => a.GameTitle)
                : query.OrderBy(a => a.GameTitle),

            MediaAssetSortBy.Resolution => filter.SortDescending
                ? query.OrderByDescending(a => a.Height)
                : query.OrderBy(a => a.Height),

            MediaAssetSortBy.LastViewedAt => filter.SortDescending
                ? query.OrderByDescending(a => a.LastViewedAt)
                : query.OrderBy(a => a.LastViewedAt),

            _ => query.OrderByDescending(a => a.UploadedAt)
        };

        return query;
    }
}
```

### 6.4 Example Query Usage

```csharp
// Find all Valorant clips uploaded this week, HD or higher, sorted by views
var query = new MediaAssetQuery
{
    GameTitle = "Valorant",
    UploadedAfter = DateTime.UtcNow.AddDays(-7),
    MinHeight = 720,
    Status = ProcessingStatus.Ready,
    SortBy = MediaAssetSortBy.ViewCount,
    SortDescending = true,
    PageNumber = 1,
    PageSize = 20
};

// Search for "pentakill" across all fields
var searchQuery = new MediaAssetQuery
{
    SearchQuery = "pentakill",
    SortBy = MediaAssetSortBy.Relevance,
    PageSize = 50
};

// Get all clips from a gaming session
var sessionQuery = new MediaAssetQuery
{
    SessionId = "session-2026-02-04-12345",
    SortBy = MediaAssetSortBy.UploadedAt,
    SortDescending = false // Chronological order
};
```

---

## 7. Video Quality Control

### 7.1 Quality Presets

```csharp
namespace MediaAssetManager.Core.Enums
{
    public enum VideoQuality
    {
        Original = 0,      // No compression
        UHD4K = 2160,      // 3840x2160 @ 10 Mbps
        QHD2K = 1440,      // 2560x1440 @ 6 Mbps
        FullHD = 1080,     // 1920x1080 @ 5 Mbps
        HD = 720,          // 1280x720 @ 2.5 Mbps
        SD = 480           // 854x480 @ 1 Mbps
    }

    public static class VideoQualityPresets
    {
        public static readonly Dictionary<VideoQuality, QualityProfile> Profiles = new()
        {
            [VideoQuality.UHD4K] = new QualityProfile
            {
                Width = 3840,
                Height = 2160,
                BitrateKbps = 10_000,
                AudioBitrateKbps = 192,
                CRF = 22,
                Preset = "medium"
            },
            [VideoQuality.QHD2K] = new QualityProfile
            {
                Width = 2560,
                Height = 1440,
                BitrateKbps = 6_000,
                AudioBitrateKbps = 192,
                CRF = 23,
                Preset = "medium"
            },
            [VideoQuality.FullHD] = new QualityProfile
            {
                Width = 1920,
                Height = 1080,
                BitrateKbps = 5_000,
                AudioBitrateKbps = 128,
                CRF = 23,
                Preset = "medium"
            },
            [VideoQuality.HD] = new QualityProfile
            {
                Width = 1280,
                Height = 720,
                BitrateKbps = 2_500,
                AudioBitrateKbps = 128,
                CRF = 24,
                Preset = "fast"
            },
            [VideoQuality.SD] = new QualityProfile
            {
                Width = 854,
                Height = 480,
                BitrateKbps = 1_000,
                AudioBitrateKbps = 96,
                CRF = 25,
                Preset = "fast"
            }
        };
    }

    public record QualityProfile
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public int BitrateKbps { get; init; }
        public int AudioBitrateKbps { get; init; }
        public int CRF { get; init; } // Constant Rate Factor (18-28)
        public string Preset { get; init; } = "medium"; // ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow
    }
}
```

### 7.2 Automatic Quality Selection

```csharp
public class SmartQualitySelector
{
    public static VideoQuality SelectOptimalQuality(VideoMetadata metadata, UserRole userRole)
    {
        // Premium users: keep original quality
        if (userRole == UserRole.Premium)
            return VideoQuality.Original;

        // For standard users: smart downscaling
        return metadata.Height switch
        {
            >= 2160 => VideoQuality.FullHD,  // 4K -> 1080p (50% reduction)
            >= 1440 => VideoQuality.HD,      // 2K -> 720p (50% reduction)
            >= 1080 => VideoQuality.HD,      // 1080p -> 720p
            >= 720 => VideoQuality.HD,       // Already HD, keep it
            _ => VideoQuality.Original       // Don't upscale SD content
        };
    }

    public static bool ShouldCompress(VideoMetadata metadata, UserRole userRole)
    {
        if (userRole == UserRole.Premium)
            return false;

        // Compress if:
        // 1. File is larger than 100MB
        // 2. Video is higher than 720p
        // 3. Bitrate is excessive (>6 Mbps for 1080p)

        var fileSizeMB = metadata.FileSizeBytes / 1024.0 / 1024.0;
        var isLarge = fileSizeMB > 100;
        var isHighRes = metadata.Height > 720;
        var isHighBitrate = metadata.BitrateKbps > 6000;

        return isLarge || isHighRes || isHighBitrate;
    }
}
```

### 7.3 Multi-Quality Encoding (Netflix-style)

For advanced use case, encode multiple qualities:

```csharp
public class AdaptiveQualityService
{
    public async Task<List<EncodedVariant>> GenerateAdaptiveVariantsAsync(
        string inputPath,
        int assetId)
    {
        var variants = new List<EncodedVariant>();
        var metadata = await _metadataService.ExtractMetadataAsync(inputPath);

        // Generate variants based on original resolution
        var targetQualities = DetermineTargetQualities(metadata.Height);

        foreach (var quality in targetQualities)
        {
            var outputPath = Path.Combine(
                Path.GetTempPath(),
                $"{assetId}_{quality}.mp4"
            );

            var result = await _compressionService.CompressVideoAsync(
                inputPath,
                outputPath,
                quality
            );

            if (result.Success)
            {
                // Upload variant to B2
                var storagePath = $"variants/{assetId}/{quality}.mp4";
                await using var stream = File.OpenRead(outputPath);
                var (_, fileName, fileSize) = await _storageService.UploadFileAsync(
                    stream,
                    storagePath,
                    "video/mp4"
                );

                variants.Add(new EncodedVariant
                {
                    Quality = quality,
                    StoragePath = fileName,
                    FileSizeBytes = fileSize
                });

                File.Delete(outputPath);
            }
        }

        return variants;
    }

    private List<VideoQuality> DetermineTargetQualities(int originalHeight)
    {
        var qualities = new List<VideoQuality>();

        if (originalHeight >= 2160) qualities.Add(VideoQuality.UHD4K);
        if (originalHeight >= 1440) qualities.Add(VideoQuality.QHD2K);
        if (originalHeight >= 1080) qualities.Add(VideoQuality.FullHD);
        if (originalHeight >= 720) qualities.Add(VideoQuality.HD);
        qualities.Add(VideoQuality.SD); // Always provide SD fallback

        return qualities;
    }
}
```

---

## 8. Authentication System (Client/Secret OAuth)

### 8.1 OAuth Flow

```
┌─────────────┐                                  ┌─────────────┐
│   Client    │                                  │  API Server │
│ Application │                                  │             │
└──────┬──────┘                                  └──────┬──────┘
       │                                                │
       │  1. POST /api/auth/token                      │
       │     { clientId, clientSecret }                │
       ├──────────────────────────────────────────────>│
       │                                                │
       │                                                │  2. Validate
       │                                                │     credentials
       │                                                │
       │  3. 200 OK                                     │
       │     { accessToken, expiresIn, role }           │
       │<──────────────────────────────────────────────┤
       │                                                │
       │  4. GET /api/mediaassets                      │
       │     Authorization: Bearer {accessToken}       │
       ├──────────────────────────────────────────────>│
       │                                                │
       │                                                │  5. Verify JWT
       │                                                │     & check role
       │                                                │
       │  6. 200 OK                                     │
       │     [ ... assets ... ]                         │
       │<──────────────────────────────────────────────┤
       │                                                │
```

### 8.2 Auth Entities (Already in User entity)

```csharp
public class User
{
    // ... existing properties ...

    // OAuth Client Credentials
    public string? ClientId { get; set; }
    public string? ClientSecretHash { get; set; } // BCrypt hashed
    public DateTime? ClientSecretCreatedAt { get; set; }
    public DateTime? ClientSecretExpiresAt { get; set; } // Optional expiration

    // Role & Permissions
    public UserRole Role { get; set; } = UserRole.User;

    // Rate limiting
    public int RateLimitPerHour { get; set; } = 100; // API calls per hour
    public DateTime? LastApiCallAt { get; set; }
}
```

### 8.3 Authentication Service

**File**: `MediaAssetManager.Services/AuthenticationService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MediaAssetManager.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TokenResponse?> AuthenticateAsync(string clientId, string clientSecret)
        {
            try
            {
                var user = await _userRepository.GetByClientIdAsync(clientId);

                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Authentication failed: Invalid client ID {ClientId}", clientId);
                    return null;
                }

                // Check if client secret has expired
                if (user.ClientSecretExpiresAt.HasValue &&
                    user.ClientSecretExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Authentication failed: Expired client secret for {ClientId}", clientId);
                    return null;
                }

                // Verify client secret (BCrypt)
                if (!BCrypt.Net.BCrypt.Verify(clientSecret, user.ClientSecretHash))
                {
                    _logger.LogWarning("Authentication failed: Invalid client secret for {ClientId}", clientId);
                    return null;
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {UserId} authenticated successfully", user.UserId);

                return new TokenResponse
                {
                    AccessToken = token,
                    TokenType = "Bearer",
                    ExpiresIn = 3600, // 1 hour
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error for client {ClientId}", clientId);
                return null;
            }
        }

        public async Task<(string ClientId, string ClientSecret)> GenerateClientCredentialsAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new InvalidOperationException("User not found");

            // Generate client ID (URL-safe)
            var clientId = $"client_{userId}_{Guid.NewGuid():N}";

            // Generate secure client secret (32 bytes = 256 bits)
            var clientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // Hash the secret for storage
            var secretHash = BCrypt.Net.BCrypt.HashPassword(clientSecret, workFactor: 12);

            // Update user
            user.ClientId = clientId;
            user.ClientSecretHash = secretHash;
            user.ClientSecretCreatedAt = DateTime.UtcNow;
            // Optional: Set expiration (e.g., 1 year)
            user.ClientSecretExpiresAt = DateTime.UtcNow.AddYears(1);

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Generated client credentials for user {UserId}", userId);

            // Return plaintext secret ONCE (never stored in plaintext)
            return (clientId, clientSecret);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT secret key not configured");
            var issuer = jwtSettings["Issuer"] ?? "MediaAssetManager";
            var audience = jwtSettings["Audience"] ?? "MediaAssetManagerAPI";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("client_id", user.ClientId ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public record TokenResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string TokenType { get; init; } = "Bearer";
        public int ExpiresIn { get; init; }
        public string Role { get; init; } = string.Empty;
    }
}
```

### 8.4 Auth Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> GetToken([FromBody] TokenRequest request)
    {
        var result = await _authService.AuthenticateAsync(
            request.ClientId,
            request.ClientSecret
        );

        if (result == null)
        {
            return Unauthorized(new { error = "invalid_client" });
        }

        return Ok(result);
    }

    [HttpPost("credentials")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ClientCredentialsResponse>> GenerateCredentials(
        [FromBody] GenerateCredentialsRequest request)
    {
        var (clientId, clientSecret) = await _authService.GenerateClientCredentialsAsync(
            request.UserId
        );

        return Ok(new ClientCredentialsResponse
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Message = "Store the client secret securely. It will not be shown again."
        });
    }
}

public record TokenRequest
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}

public record ClientCredentialsResponse
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
```

### 8.5 JWT Configuration (appsettings.json)

```json
{
	"JwtSettings": {
		"SecretKey": "<generate-secure-key-256-bits>",
		"Issuer": "MediaAssetManager",
		"Audience": "MediaAssetManagerAPI",
		"ExpirationHours": 1
	}
}
```

### 8.6 Startup Configuration

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new Exception("JWT key not configured"))
            )
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministratorRole",
        policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequirePremiumRole",
        policy => policy.RequireRole("Premium", "Administrator"));
});

// Add after app.Build()
app.UseAuthentication();
app.UseAuthorization();
```

---

## 9. CDN Strategy (Cost-Effective)

### 9.1 Options Analysis

| CDN Solution                  | Cost                        | Pros                                             | Cons                                     |
| ----------------------------- | --------------------------- | ------------------------------------------------ | ---------------------------------------- |
| **Cloudflare CDN**            | Free tier + $20/month (Pro) | Global edge network, DDoS protection, free SSL   | Bandwidth limits on free tier            |
| **Backblaze B2 + Cloudflare** | $0.005/GB + Free CDN        | Cheapest storage + free bandwidth via Cloudflare | Setup complexity                         |
| **BunnyCDN**                  | $0.01/GB (pay-as-you-go)    | Simple setup, affordable, 114 PoPs               | Less known brand                         |
| **AWS CloudFront + S3**       | $0.085/GB + storage         | Enterprise-grade, reliable                       | Expensive for high traffic               |
| **Direct B2 (no CDN)**        | $0.01/GB download           | No additional setup                              | Slower for global users, no edge caching |

**Recommended**: **Backblaze B2 + Cloudflare CDN** (Bandwidth Alliance)

### 9.2 Backblaze B2 + Cloudflare Setup

#### Step 1: Enable Cloudflare Caching

1. Point custom domain (e.g., `cdn.yourdomain.com`) to B2 bucket via Cloudflare DNS
2. Set CNAME record: `cdn.yourdomain.com` → `s3.eu-central-003.backblazeb2.com`
3. Enable "Orange Cloud" in Cloudflare (proxied)

#### Step 2: Cloudflare Page Rules

```
Rule 1: Cache Everything
URL: cdn.yourdomain.com/*
Settings:
  - Cache Level: Cache Everything
  - Edge Cache TTL: 1 month
  - Browser Cache TTL: 1 day

Rule 2: Bypass Cache for Admin
URL: cdn.yourdomain.com/admin/*
Settings:
  - Cache Level: Bypass
```

#### Step 3: Update Storage Service to Use CDN

```csharp
public class B2StorageService : IStorageService
{
    private readonly string _cdnBaseUrl;

    public B2StorageService(IConfiguration configuration)
    {
        _cdnBaseUrl = configuration["B2:CdnBaseUrl"]
            ?? throw new ArgumentNullException("B2:CdnBaseUrl");
        // e.g., "https://cdn.yourdomain.com"
    }

    public string GetCdnUrl(string storagePath)
    {
        // Public CDN URL (cached by Cloudflare)
        return $"{_cdnBaseUrl}/{storagePath}";
    }

    public async Task<string> GetSignedDownloadUrlAsync(
        string fileName,
        int expirationSeconds = 3600,
        bool useCdn = false)
    {
        if (useCdn)
        {
            // Return CDN URL (public, cached)
            return GetCdnUrl(fileName);
        }

        // Return signed B2 URL (private, temporary)
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Expires = DateTime.UtcNow.AddSeconds(expirationSeconds)
        };

        return await _s3Client.GetPreSignedURLAsync(request);
    }
}
```

#### Step 4: Configuration

```json
{
	"B2": {
		"KeyId": "<secret>",
		"KeySecret": "<secret>",
		"BucketName": "media-asset-manager-storage-dev",
		"Endpoint": "https://s3.eu-central-003.backblazeb2.com",
		"CdnBaseUrl": "https://cdn.yourdomain.com"
	}
}
```

### 9.3 Cost Calculation

**Example**: 10,000 video views/month, avg 100MB per video

| Service                        | Cost Calculation        | Monthly Cost    |
| ------------------------------ | ----------------------- | --------------- |
| B2 Storage (1TB)               | $0.006/GB × 1000 GB     | $6.00           |
| B2 Bandwidth (with Cloudflare) | $0 (Bandwidth Alliance) | $0.00           |
| Cloudflare CDN                 | Free tier               | $0.00           |
| **Total**                      |                         | **$6.00/month** |

**Without CDN** (direct B2):

- B2 Bandwidth: 1,000 GB × $0.01/GB = **$10.00**
- **Total**: $16.00/month

**Savings**: ~$10/month (62.5% cheaper with CDN)

### 9.4 Alternative: BunnyCDN

If you need more control:

```json
{
	"BunnyCDN": {
		"ApiKey": "<secret>",
		"StorageZoneName": "media-assets",
		"PullZoneUrl": "https://mediaassets.b-cdn.net"
	}
}
```

**Pricing**: $0.01/GB (all regions), $1/month base fee

---

## 10. Favorites & Playlists

### 10.1 Favorites Service

**File**: `MediaAssetManager.Services/FavoriteService.cs`

```csharp
namespace MediaAssetManager.Services
{
    public interface IFavoriteService
    {
        Task<bool> AddFavoriteAsync(int userId, int assetId);
        Task<bool> RemoveFavoriteAsync(int userId, int assetId);
        Task<bool> IsFavoritedAsync(int userId, int assetId);
        Task<PagedResult<MediaAsset>> GetUserFavoritesAsync(int userId, int page = 1, int pageSize = 20);
    }

    public class FavoriteService : IFavoriteService
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IMediaAssetRepository _assetRepository;

        public FavoriteService(
            IFavoriteRepository favoriteRepository,
            IMediaAssetRepository assetRepository)
        {
            _favoriteRepository = favoriteRepository;
            _assetRepository = assetRepository;
        }

        public async Task<bool> AddFavoriteAsync(int userId, int assetId)
        {
            // Check if already favorited
            if (await IsFavoritedAsync(userId, assetId))
                return false;

            var favorite = new Favorite
            {
                UserId = userId,
                AssetId = assetId,
                CreatedAt = DateTime.UtcNow
            };

            await _favoriteRepository.AddAsync(favorite);

            // Increment favorite count on asset
            var asset = await _assetRepository.GetByIdAsync(assetId);
            if (asset != null)
            {
                asset.FavoriteCount++;
                await _assetRepository.UpdateAsync(asset);
            }

            return true;
        }

        public async Task<bool> RemoveFavoriteAsync(int userId, int assetId)
        {
            var favorite = await _favoriteRepository.GetByUserAndAssetAsync(userId, assetId);
            if (favorite == null)
                return false;

            await _favoriteRepository.DeleteAsync(favorite);

            // Decrement favorite count
            var asset = await _assetRepository.GetByIdAsync(assetId);
            if (asset != null && asset.FavoriteCount > 0)
            {
                asset.FavoriteCount--;
                await _assetRepository.UpdateAsync(asset);
            }

            return true;
        }

        public async Task<bool> IsFavoritedAsync(int userId, int assetId)
        {
            var favorite = await _favoriteRepository.GetByUserAndAssetAsync(userId, assetId);
            return favorite != null;
        }

        public async Task<PagedResult<MediaAsset>> GetUserFavoritesAsync(
            int userId,
            int page = 1,
            int pageSize = 20)
        {
            return await _favoriteRepository.GetUserFavoritesPagedAsync(
                userId,
                page,
                pageSize
            );
        }
    }
}
```

### 10.2 Playlist Service

```csharp
namespace MediaAssetManager.Services
{
    public interface IPlaylistService
    {
        Task<Playlist> CreatePlaylistAsync(int userId, string name, string? description, bool isPublic);
        Task<bool> AddToPlaylistAsync(int playlistId, int assetId, int userId);
        Task<bool> RemoveFromPlaylistAsync(int playlistId, int assetId, int userId);
        Task<bool> DeletePlaylistAsync(int playlistId, int userId);
        Task<PagedResult<Playlist>> GetUserPlaylistsAsync(int userId, int page = 1, int pageSize = 20);
        Task<PagedResult<MediaAsset>> GetPlaylistAssetsAsync(int playlistId, int page = 1, int pageSize = 50);
        Task<bool> ReorderPlaylistAsync(int playlistId, Dictionary<int, int> assetSortOrders, int userId);
    }

    public class PlaylistService : IPlaylistService
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IMediaAssetRepository _assetRepository;

        public PlaylistService(
            IPlaylistRepository playlistRepository,
            IMediaAssetRepository assetRepository)
        {
            _playlistRepository = playlistRepository;
            _assetRepository = assetRepository;
        }

        public async Task<Playlist> CreatePlaylistAsync(
            int userId,
            string name,
            string? description,
            bool isPublic)
        {
            var playlist = new Playlist
            {
                UserId = userId,
                Name = name,
                Description = description,
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _playlistRepository.AddAsync(playlist);
        }

        public async Task<bool> AddToPlaylistAsync(int playlistId, int assetId, int userId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null || playlist.UserId != userId)
                return false;

            // Check if already in playlist
            if (await _playlistRepository.IsAssetInPlaylistAsync(playlistId, assetId))
                return false;

            // Get next sort order
            var maxSortOrder = await _playlistRepository.GetMaxSortOrderAsync(playlistId);

            var playlistAsset = new PlaylistAsset
            {
                PlaylistId = playlistId,
                AssetId = assetId,
                SortOrder = maxSortOrder + 1,
                AddedAt = DateTime.UtcNow
            };

            await _playlistRepository.AddAssetAsync(playlistAsset);

            // Update playlist timestamp
            playlist.UpdatedAt = DateTime.UtcNow;
            await _playlistRepository.UpdateAsync(playlist);

            return true;
        }

        public async Task<bool> RemoveFromPlaylistAsync(int playlistId, int assetId, int userId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null || playlist.UserId != userId)
                return false;

            await _playlistRepository.RemoveAssetAsync(playlistId, assetId);

            playlist.UpdatedAt = DateTime.UtcNow;
            await _playlistRepository.UpdateAsync(playlist);

            return true;
        }

        public async Task<bool> ReorderPlaylistAsync(
            int playlistId,
            Dictionary<int, int> assetSortOrders,
            int userId)
        {
            var playlist = await _playlistRepository.GetByIdAsync(playlistId);
            if (playlist == null || playlist.UserId != userId)
                return false;

            await _playlistRepository.UpdateSortOrdersAsync(playlistId, assetSortOrders);

            playlist.UpdatedAt = DateTime.UtcNow;
            await _playlistRepository.UpdateAsync(playlist);

            return true;
        }

        // ... other methods
    }
}
```

### 10.3 API Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    [HttpPost("{assetId}")]
    public async Task<IActionResult> AddFavorite(int assetId)
    {
        var userId = GetCurrentUserId();
        var success = await _favoriteService.AddFavoriteAsync(userId, assetId);
        return success ? Ok() : Conflict(new { error = "Already favorited" });
    }

    [HttpDelete("{assetId}")]
    public async Task<IActionResult> RemoveFavorite(int assetId)
    {
        var userId = GetCurrentUserId();
        var success = await _favoriteService.RemoveFavoriteAsync(userId, assetId);
        return success ? NoContent() : NotFound();
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> GetMyFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var result = await _favoriteService.GetUserFavoritesAsync(userId, page, pageSize);
        return Ok(result.ToPaginatedResponse());
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlaylistsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PlaylistResponse>> CreatePlaylist(
        [FromBody] CreatePlaylistRequest request)
    {
        var userId = GetCurrentUserId();
        var playlist = await _playlistService.CreatePlaylistAsync(
            userId,
            request.Name,
            request.Description,
            request.IsPublic ?? true
        );
        return CreatedAtAction(nameof(GetPlaylist), new { id = playlist.PlaylistId }, playlist);
    }

    [HttpPost("{playlistId}/assets/{assetId}")]
    public async Task<IActionResult> AddToPlaylist(int playlistId, int assetId)
    {
        var userId = GetCurrentUserId();
        var success = await _playlistService.AddToPlaylistAsync(playlistId, assetId, userId);
        return success ? Ok() : BadRequest(new { error = "Cannot add to playlist" });
    }

    [HttpGet("{playlistId}/assets")]
    public async Task<ActionResult<PaginatedResponse<MediaAssetResponse>>> GetPlaylistAssets(
        int playlistId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _playlistService.GetPlaylistAssetsAsync(playlistId, page, pageSize);
        return Ok(result.ToPaginatedResponse());
    }
}
```

---

## 11. Analytics & Tracking (Heatmaps)

### 11.1 Analytics Service

**File**: `MediaAssetManager.Services/AnalyticsService.cs`

```csharp
namespace MediaAssetManager.Services
{
    public interface IAnalyticsService
    {
        Task TrackEventAsync(AssetAnalytics analyticsEvent);
        Task<Dictionary<DateTime, int>> GetUploadHeatmapAsync(int userId, DateTime startDate, DateTime endDate);
        Task<Dictionary<DateTime, int>> GetViewHeatmapAsync(int userId, DateTime startDate, DateTime endDate);
        Task<AssetAnalyticsSummary> GetAssetAnalyticsAsync(int assetId);
        Task<UserAnalyticsSummary> GetUserAnalyticsAsync(int userId);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _analyticsRepository;
        private readonly IMediaAssetRepository _assetRepository;

        public AnalyticsService(
            IAnalyticsRepository analyticsRepository,
            IMediaAssetRepository assetRepository)
        {
            _analyticsRepository = analyticsRepository;
            _assetRepository = assetRepository;
        }

        public async Task TrackEventAsync(AssetAnalytics analyticsEvent)
        {
            // Validate event
            if (analyticsEvent.AssetId <= 0)
                throw new ArgumentException("Invalid asset ID");

            // Store event (fire-and-forget for performance)
            await _analyticsRepository.AddAsync(analyticsEvent);
        }

        public async Task<Dictionary<DateTime, int>> GetUploadHeatmapAsync(
            int userId,
            DateTime startDate,
            DateTime endDate)
        {
            // Get all uploads by user in date range
            var uploads = await _assetRepository.GetUploadsByDateRangeAsync(
                userId,
                startDate,
                endDate
            );

            // Group by date and count
            var heatmap = uploads
                .GroupBy(a => a.UploadedAt.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            // Fill missing dates with 0
            return FillMissingDates(heatmap, startDate, endDate);
        }

        public async Task<Dictionary<DateTime, int>> GetViewHeatmapAsync(
            int userId,
            DateTime startDate,
            DateTime endDate)
        {
            // Get view events for user's assets
            var views = await _analyticsRepository.GetEventsByUserAssetsAsync(
                userId,
                AnalyticsEventType.View,
                startDate,
                endDate
            );

            var heatmap = views
                .GroupBy(v => v.EventTimestamp.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            return FillMissingDates(heatmap, startDate, endDate);
        }

        public async Task<AssetAnalyticsSummary> GetAssetAnalyticsAsync(int assetId)
        {
            var events = await _analyticsRepository.GetEventsByAssetAsync(assetId);

            return new AssetAnalyticsSummary
            {
                AssetId = assetId,
                TotalViews = events.Count(e => e.EventType == AnalyticsEventType.View),
                TotalDownloads = events.Count(e => e.EventType == AnalyticsEventType.Download),
                TotalShares = events.Count(e => e.EventType == AnalyticsEventType.Share),
                TotalFavorites = events.Count(e => e.EventType == AnalyticsEventType.Favorite),
                UniqueViewers = events
                    .Where(e => e.EventType == AnalyticsEventType.View && e.UserId.HasValue)
                    .Select(e => e.UserId.Value)
                    .Distinct()
                    .Count(),
                FirstViewedAt = events
                    .Where(e => e.EventType == AnalyticsEventType.View)
                    .Min(e => e.EventTimestamp),
                LastViewedAt = events
                    .Where(e => e.EventType == AnalyticsEventType.View)
                    .Max(e => e.EventTimestamp),
                ViewsByDay = events
                    .Where(e => e.EventType == AnalyticsEventType.View)
                    .GroupBy(e => e.EventTimestamp.Date)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<UserAnalyticsSummary> GetUserAnalyticsAsync(int userId)
        {
            var assets = await _assetRepository.GetByUserIdAsync(userId);
            var totalViews = assets.Sum(a => a.ViewCount);
            var totalFavorites = assets.Sum(a => a.FavoriteCount);

            return new UserAnalyticsSummary
            {
                UserId = userId,
                TotalAssets = assets.Count,
                TotalViews = totalViews,
                TotalFavorites = totalFavorites,
                TotalStorageBytes = assets.Sum(a => a.EffectiveFileSizeBytes),
                MostViewedAsset = assets.OrderByDescending(a => a.ViewCount).FirstOrDefault(),
                RecentUploads = assets.OrderByDescending(a => a.UploadedAt).Take(5).ToList()
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

    public record AssetAnalyticsSummary
    {
        public int AssetId { get; init; }
        public int TotalViews { get; init; }
        public int TotalDownloads { get; init; }
        public int TotalShares { get; init; }
        public int TotalFavorites { get; init; }
        public int UniqueViewers { get; init; }
        public DateTime? FirstViewedAt { get; init; }
        public DateTime? LastViewedAt { get; init; }
        public Dictionary<DateTime, int> ViewsByDay { get; init; } = new();
    }

    public record UserAnalyticsSummary
    {
        public int UserId { get; init; }
        public int TotalAssets { get; init; }
        public int TotalViews { get; init; }
        public int TotalFavorites { get; init; }
        public long TotalStorageBytes { get; init; }
        public MediaAsset? MostViewedAsset { get; init; }
        public List<MediaAsset> RecentUploads { get; init; } = new();
    }
}
```

### 11.2 Analytics Controller

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    [HttpGet("heatmap/uploads")]
    public async Task<ActionResult<HeatmapResponse>> GetUploadHeatmap(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = GetCurrentUserId();
        var start = startDate ?? DateTime.UtcNow.AddYears(-1);
        var end = endDate ?? DateTime.UtcNow;

        var data = await _analyticsService.GetUploadHeatmapAsync(userId, start, end);

        return Ok(new HeatmapResponse
        {
            StartDate = start,
            EndDate = end,
            Data = data
        });
    }

    [HttpGet("heatmap/views")]
    public async Task<ActionResult<HeatmapResponse>> GetViewHeatmap(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = GetCurrentUserId();
        var start = startDate ?? DateTime.UtcNow.AddYears(-1);
        var end = endDate ?? DateTime.UtcNow;

        var data = await _analyticsService.GetViewHeatmapAsync(userId, start, end);

        return Ok(new HeatmapResponse
        {
            StartDate = start,
            EndDate = end,
            Data = data
        });
    }

    [HttpGet("assets/{assetId}")]
    public async Task<ActionResult<AssetAnalyticsSummary>> GetAssetAnalytics(int assetId)
    {
        // Check ownership or public
        var asset = await _assetService.GetByIdAsync(assetId);
        if (asset == null) return NotFound();

        if (!asset.IsPublic && asset.UserId != GetCurrentUserId())
            return Forbid();

        var summary = await _analyticsService.GetAssetAnalyticsAsync(assetId);
        return Ok(summary);
    }

    [HttpGet("users/me")]
    public async Task<ActionResult<UserAnalyticsSummary>> GetMyAnalytics()
    {
        var userId = GetCurrentUserId();
        var summary = await _analyticsService.GetUserAnalyticsAsync(userId);
        return Ok(summary);
    }
}

public record HeatmapResponse
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public Dictionary<DateTime, int> Data { get; init; } = new();
}
```

### 11.3 Frontend Heatmap Visualization (React Example)

```jsx
// Using react-calendar-heatmap library
import CalendarHeatmap from 'react-calendar-heatmap';
import 'react-calendar-heatmap/dist/styles.css';

function UploadHeatmap({ userId }) {
	const [heatmapData, setHeatmapData] = useState([]);

	useEffect(() => {
		fetchHeatmap();
	}, []);

	const fetchHeatmap = async () => {
		const response = await fetch('/api/analytics/heatmap/uploads', {
			headers: { Authorization: `Bearer ${accessToken}` },
		});
		const data = await response.json();

		// Convert to heatmap format
		const formatted = Object.entries(data.data).map(([date, count]) => ({
			date: new Date(date),
			count: count,
		}));

		setHeatmapData(formatted);
	};

	return (
		<div>
			<h3>Upload Activity (Last 365 Days)</h3>
			<CalendarHeatmap
				startDate={new Date(new Date().setFullYear(new Date().getFullYear() - 1))}
				endDate={new Date()}
				values={heatmapData}
				classForValue={(value) => {
					if (!value) return 'color-empty';
					if (value.count === 0) return 'color-empty';
					if (value.count < 3) return 'color-scale-1';
					if (value.count < 6) return 'color-scale-2';
					if (value.count < 9) return 'color-scale-3';
					return 'color-scale-4';
				}}
				tooltipDataAttrs={(value) => ({
					'data-tip': value.date
						? `${value.count} uploads on ${value.date.toLocaleDateString()}`
						: 'No uploads',
				})}
			/>
		</div>
	);
}
```

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1-2)

- ✅ Entity model extensions
- ✅ Database migrations
- ✅ Video processing services setup (FFmpeg)

### Phase 2: Core Features (Week 3-4)

- ✅ Upload orchestration with background jobs
- ✅ Metadata extraction & thumbnail generation
- ✅ B2 bucket organization
- ✅ Enhanced query system

### Phase 3: Authentication (Week 5)

- ✅ Client/Secret OAuth implementation
- ✅ JWT token generation
- ✅ Role-based permissions

### Phase 4: Social Features (Week 6-7)

- ✅ Favorites system
- ✅ Playlist management
- ✅ Analytics tracking

### Phase 5: Optimization (Week 8)

- ✅ CDN integration (Cloudflare + B2)
- ✅ Video quality control & compression
- ✅ Performance tuning

### Phase 6: Polish (Week 9-10)

- ✅ Heatmap visualizations
- ✅ Advanced search & filtering
- ✅ API documentation (Swagger)
- ✅ Testing & bug fixes

---

## Next Steps

1. **Choose your priorities** from the 11 sections above
2. **Start with entities** - Run migrations to extend the database
3. **Install FFmpeg** and set up video processing services
4. **Configure Hangfire** for background job processing
5. **Implement upload endpoint** with orchestration
6. **Set up authentication** with client credentials
7. **Add social features** (favorites, playlists)
8. **Integrate CDN** (Cloudflare + B2)
9. **Build analytics** & tracking
10. **Test end-to-end** workflow

---

## Resources

- **FFmpeg Documentation**: https://ffmpeg.org/documentation.html
- **FFMpegCore Library**: https://github.com/rosenbjerg/FFMpegCore
- **Hangfire**: https://www.hangfire.io/
- **Backblaze B2**: https://www.backblaze.com/b2/docs/
- **Cloudflare CDN**: https://developers.cloudflare.com/
- **JWT Authentication**: https://jwt.io/

---

**Questions or need clarification on any section?** Let me know which area you'd like to dive deeper into!
