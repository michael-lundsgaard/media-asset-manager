namespace MediaAssetManager.Core.Queries
{
    /// <summary>
    /// Represents a set of criteria for querying playlists, 
    /// including filters, sorting options, and pagination settings.
    /// </summary>
    public class PlaylistQuery
    {
        public int? UserId { get; set; }
        public bool? IsPublic { get; set; }
        public string? Name { get; set; }

        public PlaylistSortBy SortBy { get; set; } = PlaylistSortBy.CreatedAt;
        public bool SortDescending { get; set; } = true;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public enum PlaylistSortBy
    {
        CreatedAt,
        Name,
        IsPublic
    }
}