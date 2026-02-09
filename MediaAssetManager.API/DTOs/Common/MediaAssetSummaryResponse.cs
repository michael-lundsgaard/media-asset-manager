namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// Lightweight media asset information for nested responses (e.g., playlist items).
    /// Contains only core properties, NO navigation properties or collections.
    /// </summary>
    public class MediaAssetSummaryResponse
    {
        public int AssetId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public int ViewCount { get; set; }
    }
}
