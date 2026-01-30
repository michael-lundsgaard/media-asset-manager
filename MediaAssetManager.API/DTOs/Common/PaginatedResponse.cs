namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// API pagination response wrapper with HATEOAS links and metadata
    /// </summary>
    public class PaginatedResponse<T>
    {
        public required IReadOnlyList<T> Items { get; set; }
        public required int TotalCount { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalPages { get; set; }
    }

    /// <summary>
    /// HATEOAS links for pagination navigation
    /// </summary>
    /// <remarks>
    /// TO BE USED IN FUTURE EXTENSIONS - currently not included in PaginatedResponse
    /// </remarks>
    public class PaginationLinks
    {
        public required string Self { get; set; }
        public required string First { get; set; }
        public string? Previous { get; set; }
        public string? Next { get; set; }
        public required string Last { get; set; }
    }
}
