namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// Technical video metadata - separated from MediaAsset for clean separation of concerns
    /// Optional 1:0..1 relationship (only exists for video files)
    /// </summary>
    public class VideoMetadata
    {
        public int VideoMetadataId { get; set; }

        // === FOREIGN KEY ===
        public int AssetId { get; set; }

        // === TECHNICAL SPECIFICATIONS ===
        public int DurationSeconds { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal FrameRate { get; set; }

        // === CODEC INFORMATION (from V1) ===
        public string? Codec { get; set; }          // e.g., "h264", "hevc"
        public int? BitrateKbps { get; set; }
        public string? AudioCodec { get; set; }     // e.g., "aac", "mp3"

        // === NAVIGATION PROPERTY ===
        public MediaAsset Asset { get; set; } = null!;
    }
}
