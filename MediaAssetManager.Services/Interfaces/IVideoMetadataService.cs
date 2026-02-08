namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Service for extracting technical metadata from video files using FFprobe
    /// </summary>
    public interface IVideoMetadataService
    {
        /// <summary>
        /// Extracts video metadata from a file path.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the video metadata.</returns>
        Task<VideoMetadataResult> ExtractMetadataAsync(string filePath);

        /// <summary>
        /// Validates if a file is a supported video format.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the file is a valid video; otherwise, false.</returns>
        Task<bool> IsValidVideoAsync(string filePath);

        /// <summary>
        /// Gets the duration of a video file in seconds.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the duration in seconds.</returns>
        Task<int> GetDurationSecondsAsync(string filePath);
    }

    /// <summary>
    /// Result of video metadata extraction
    /// </summary>
    public record VideoMetadataResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        // Video properties
        public int DurationSeconds { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public decimal FrameRate { get; init; }
        public string? Codec { get; init; }
        public int? BitrateKbps { get; init; }
        public string? AudioCodec { get; init; }
    }
}
