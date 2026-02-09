namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// Lightweight playlist information for nested responses.
    /// Does NOT include Items collection to prevent circular references.
    /// </summary>
    public class PlaylistSummaryResponse
    {
        public int PlaylistId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
