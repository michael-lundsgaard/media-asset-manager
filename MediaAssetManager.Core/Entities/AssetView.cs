namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// Analytics tracking for asset views - used for heatmaps and view statistics
    /// Separate table optimized for time-series queries and potential partitioning
    /// </summary>
    public class AssetView
    {
        public long ViewId { get; set; }  // Long for high volume
        public int AssetId { get; set; }
        public int? UserId { get; set; }  // Null for anonymous views
        public DateTime ViewedAt { get; set; }

        // === NAVIGATION PROPERTIES ===
        public MediaAsset Asset { get; set; } = null!;
        public User? User { get; set; }
    }
}
