namespace MediaAssetManager.Core.Queries
{
    /// <summary>
    /// Represents a set of criteria for querying media assets, 
    /// including filters, sorting options, and pagination settings.
    /// </summary>
    /// <remarks>
    /// Use this class to specify search parameters when retrieving media assets from a data source.
    /// Set the desired properties to filter results by file name, title, file size, upload date, and to control sorting
    /// and paging behavior. All filter properties are optional; only set the properties relevant to your
    /// query.
    /// </remarks>
    public class MediaAssetQuery
    {
        public string? FileName { get; set; }
        public string? Title { get; set; }
        public long? MinFileSizeBytes { get; set; }
        public long? MaxFileSizeBytes { get; set; }
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }
        public int? PlaylistId { get; set; }

        public MediaAssetSortBy SortBy { get; set; } = MediaAssetSortBy.UploadedAt;
        public bool SortDescending { get; set; } = true;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Set of navigation properties to eagerly load via Include().
        /// Use case-insensitive values: "user", "videoMetadata", etc.
        /// </summary>
        public HashSet<string>? Expand { get; set; }
    }
}
