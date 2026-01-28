namespace MediaAssetManager.API.DTOs
{
    /// <summary>
    /// Response DTO for media asset information
    /// </summary>
    public class MediaAssetResponseDto
    {
        public int AssetId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? Title { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
