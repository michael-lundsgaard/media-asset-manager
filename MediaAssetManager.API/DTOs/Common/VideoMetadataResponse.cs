namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// Video metadata response DTO.
    /// Contains technical video specifications without navigation properties.
    /// </summary>
    public class VideoMetadataResponse
    {
        public int VideoMetadataId { get; set; }
        public int AssetId { get; set; }
        public int DurationSeconds { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal FrameRate { get; set; }
        public string? Codec { get; set; }
        public int? BitrateKbps { get; set; }
        public string? AudioCodec { get; set; }
    }
}
