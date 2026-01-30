namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// Standard API error response model
    /// </summary>
    public class ErrorResponse
    {
        public required string Message { get; set; }
        public string? Details { get; set; }
        public required int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
