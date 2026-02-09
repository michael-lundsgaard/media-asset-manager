using MediaAssetManager.API.DTOs.Common;

namespace MediaAssetManager.API.DTOs
{
    /// <summary>
    /// Response DTO for media asset information.
    /// Supports conditional expansion of navigation properties via query parameters.
    /// </summary>
    public class MediaAssetResponse
    {
        public int AssetId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? Title { get; set; }
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Total number of views for this asset (replaces Views collection).
        /// </summary>
        public int ViewCount { get; set; }

        /// <summary>
        /// User who uploaded the asset. Populated when ?expand=user is specified.
        /// </summary>
        public UserSummaryResponse? User { get; set; }

        /// <summary>
        /// Video metadata. Populated when ?expand=videoMetadata is specified.
        /// </summary>
        public VideoMetadataResponse? VideoMetadata { get; set; }
    }
}
