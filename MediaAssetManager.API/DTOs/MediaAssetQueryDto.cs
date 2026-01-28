using MediaAssetManager.Core.Queries;
using System.ComponentModel.DataAnnotations;

namespace MediaAssetManager.API.DTOs
{
    /// <summary>
    /// DTO for querying media assets with filtering, sorting, and pagination options.
    /// </summary>
    public class MediaAssetQueryDto
    {
        public string? FileName { get; set; }
        public string? Title { get; set; }
        public long? MinFileSizeBytes { get; set; }
        public long? MaxFileSizeBytes { get; set; }
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }

        public MediaAssetSortBy SortBy { get; set; } = MediaAssetSortBy.UploadedAt;
        public bool SortDescending { get; set; } = true;

        [Range(0, int.MaxValue)]
        public int Skip { get; set; } = 0;
        [Range(1, 1_000)] // Prevent excessive page sizes
        public int Take { get; set; } = 50;
    }
}
