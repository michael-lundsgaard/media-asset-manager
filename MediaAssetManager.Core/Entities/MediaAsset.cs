using MediaAssetManager.Core.Enums;

namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// Core media asset entity - represents a user-uploaded media file (video, image, or audio)
    /// Follows lean entity design with technical metadata separated into VideoMetadata
    /// </summary>
    public class MediaAsset
    {
        // === PRIMARY KEY ===
        public int AssetId { get; set; }

        // === USER & OWNERSHIP ===
        public int? UserId { get; set; }

        // === FILE IDENTIFICATION ===
        public string FileName { get; set; } = string.Empty;         // Storage filename (unique)
        public string OriginalFileName { get; set; } = string.Empty; // User's original filename
        public long FileSizeBytes { get; set; }
        public MediaType MediaType { get; set; } = MediaType.Video;

        // === CONTENT HASH (Duplicate Detection from V1) ===
        /// <summary>
        /// SHA256 hash of file content for duplicate detection
        /// </summary>
        public string ContentHash { get; set; } = string.Empty;

        // === USER-FACING CONTENT ===
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GameTitle { get; set; }
        public List<string> Tags { get; set; } = new(); // Simple string array stored as JSON

        // === STORAGE ===
        /// <summary>
        /// Primary storage path in B2 (compressed version if compression occurred, otherwise original)
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
        public bool IsCompressed { get; set; } = false;

        // === PROCESSING ===
        public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessingError { get; set; }

        public MediaAssetLifecycle Lifecycle { get; set; } = MediaAssetLifecycle.Active;


        // === VISIBILITY ===
        public bool IsPublic { get; set; } = true;

        // === TIMESTAMPS ===
        public DateTime UploadedAt { get; set; }
        public DateTime? LastViewedAt { get; set; }

        // === SIMPLE ANALYTICS (Cached Counters) ===
        public int ViewCount { get; set; } = 0;

        // === NAVIGATION PROPERTIES ===
        public User? User { get; set; } = null;
        public VideoMetadata? VideoMetadata { get; set; }  // Optional 1:0..1 for video files
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<PlaylistItem> PlaylistItems { get; set; } = new List<PlaylistItem>();
        public ICollection<AssetView> Views { get; set; } = new List<AssetView>();

        // === COMPUTED PROPERTIES ===
        /// <summary>
        /// Friendly resolution label (4K, 1080p, 720p, SD)
        /// </summary>
        public string ResolutionLabel => VideoMetadata?.Height switch
        {
            >= 2160 => "4K",
            >= 1080 => "1080p",
            >= 720 => "720p",
            >= 480 => "480p",
            _ => "SD"
        };

        public void MarkOrphaned()
        {
            Lifecycle = MediaAssetLifecycle.Orphaned;
            IsPublic = false;
        }

        public void Archive()
        {
            Lifecycle = MediaAssetLifecycle.Archived;
            IsPublic = false;
        }

        public void Restore()
        {
            Lifecycle = MediaAssetLifecycle.Active;
        }

        public void MarkForDeletion()
        {
            Lifecycle = MediaAssetLifecycle.PendingDelete;
            IsPublic = false;
        }
    }
}
