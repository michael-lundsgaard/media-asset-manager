namespace MediaAssetManager.Core.Entities
{
    public class MediaAsset
    {
        public int AssetId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? Title {  get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
