namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Service for generating thumbnails and preview images from video files
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// Generates a thumbnail image from a video file at a specific timestamp.
        /// </summary>
        /// <param name="videoPath">The path to the video file.</param>
        /// <param name="outputPath">The path where the thumbnail should be saved.</param>
        /// <param name="timestampSeconds">The timestamp in seconds where to capture the thumbnail (default: 3 seconds).</param>
        /// <param name="width">The width of the thumbnail in pixels (default: 1280).</param>
        /// <param name="height">The height of the thumbnail in pixels (default: 720).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the thumbnail generation.</returns>
        Task<ThumbnailResult> GenerateThumbnailAsync(
            string videoPath,
            string outputPath,
            int timestampSeconds = 3,
            int width = 1280,
            int height = 720);

        /// <summary>
        /// Generates a thumbnail and returns it as a stream (for in-memory processing).
        /// </summary>
        /// <param name="videoPath">The path to the video file.</param>
        /// <param name="timestampSeconds">The timestamp in seconds where to capture the thumbnail.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a stream with the thumbnail image.</returns>
        Task<Stream> GenerateThumbnailStreamAsync(string videoPath, int timestampSeconds = 3);
    }

    /// <summary>
    /// Result of thumbnail generation
    /// </summary>
    public record ThumbnailResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public string? OutputPath { get; init; }
        public long FileSizeBytes { get; init; }
    }
}
