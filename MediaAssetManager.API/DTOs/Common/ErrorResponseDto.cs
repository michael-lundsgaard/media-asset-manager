namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// Standard error response structure
    /// </summary>
    public class ErrorResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
