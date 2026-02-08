namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Orchestrates the complete video processing pipeline: metadata extraction, thumbnail generation, and compression
    /// </summary>
    public interface IVideoProcessingService
    {
        /// <summary>
        /// Processes a video file completely: extracts metadata, generates thumbnail, and optionally compresses.
        /// This is the main entry point for video processing after upload.
        /// </summary>
        /// <param name="inputPath">The path to the input video file.</param>
        /// <param name="shouldCompress">Whether to compress the video (default: true).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the complete processing result.</returns>
        Task<VideoProcessingResult> ProcessVideoAsync(string inputPath, bool shouldCompress = true);
    }

    /// <summary>
    /// Complete result of video processing pipeline
    /// </summary>
    public record VideoProcessingResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        // Metadata
        public int DurationSeconds { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public decimal FrameRate { get; init; }
        public string? Codec { get; init; }
        public int? BitrateKbps { get; init; }
        public string? AudioCodec { get; init; }

        // Processed files
        public string VideoPath { get; init; } = string.Empty;  // Final video path (compressed or original)
        public long VideoSizeBytes { get; init; }
        public bool WasCompressed { get; init; }

        public string? ThumbnailPath { get; init; }
        public long? ThumbnailSizeBytes { get; init; }

        // Processing stats
        public int ProcessingTimeSeconds { get; init; }
    }
}
