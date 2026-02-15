using MediaAssetManager.API.Constants;
using MediaAssetManager.API.Validation;
using MediaAssetManager.Core.Queries;
using System.ComponentModel.DataAnnotations;

namespace MediaAssetManager.API.DTOs
{
    /// <summary>
    /// API request model for querying media assets with filtering, sorting, and pagination options.
    /// Contains API-specific validation and maps to Core.Queries.MediaAssetQuery.
    /// </summary>
    public class MediaAssetQueryRequest
    {
        public string? FileName { get; set; }
        public string? Title { get; set; }
        public long? MinFileSizeBytes { get; set; }
        public long? MaxFileSizeBytes { get; set; }
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }

        public MediaAssetSortBy SortBy { get; set; } = MediaAssetSortBy.UploadedAt;
        public bool SortDescending { get; set; } = true;

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 1_000)]
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Comma-separated list of navigation properties to include in the response.
        /// Supported values: "user", "videoMetadata"
        /// Example: ?expand=user,videoMetadata
        /// </summary>
        [AllowedExpandValues(MediaAssetExpandOptions.User, MediaAssetExpandOptions.VideoMetadata)]
        public string[]? Expand { get; set; }
    }
}
