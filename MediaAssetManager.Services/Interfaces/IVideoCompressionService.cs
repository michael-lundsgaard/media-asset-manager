namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Service for video compression and optimization
    /// </summary>
    public interface IVideoCompressionService
    {
        /// <summary>
        /// Compresses a video file to a target quality/resolution.
        /// </summary>
        /// <param name="inputPath">The path to the input video file.</param>
        /// <param name="outputPath">The path where the compressed video should be saved.</param>
        /// <param name="targetHeight">The target height in pixels (e.g., 720 for 720p). Width will be auto-calculated to maintain aspect ratio.</param>
        /// <param name="targetBitrateKbps">The target bitrate in kbps (optional, uses smart default based on resolution).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the compression.</returns>
        Task<CompressionResult> CompressVideoAsync(
            string inputPath,
            string outputPath,
            int targetHeight = 720,
            int? targetBitrateKbps = null);

        /// <summary>
        /// Determines if a video should be compressed based on its properties.
        /// </summary>
        /// <param name="fileSizeBytes">The file size in bytes.</param>
        /// <param name="width">The video width.</param>
        /// <param name="height">The video height.</param>
        /// <returns>True if the video should be compressed; otherwise, false.</returns>
        bool ShouldCompress(long fileSizeBytes, int width, int height);

        /// <summary>
        /// Calculates the optimal target height for compression based on source dimensions.
        /// </summary>
        /// <param name="sourceWidth">The source video width.</param>
        /// <param name="sourceHeight">The source video height.</param>
        /// <returns>The recommended target height (720, 1080, etc.).</returns>
        int GetOptimalTargetHeight(int sourceWidth, int sourceHeight);
    }

    /// <summary>
    /// Result of video compression
    /// </summary>
    public record CompressionResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public string? OutputPath { get; init; }
        public long OriginalSizeBytes { get; init; }
        public long CompressedSizeBytes { get; init; }
        public decimal CompressionRatio { get; init; } // e.g., 0.45 = 45% of original size
        public int ProcessingTimeSeconds { get; init; }
    }
}
