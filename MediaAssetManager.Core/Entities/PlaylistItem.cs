namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// Many-to-many relationship between Playlists and MediaAssets
    /// </summary>
    public class PlaylistItem
    {
        public int PlaylistItemId { get; set; }
        public int PlaylistId { get; set; }
        public int AssetId { get; set; }
        public DateTime AddedAt { get; set; }

        // === NAVIGATION PROPERTIES ===
        public Playlist Playlist { get; set; } = null!;
        public MediaAsset Asset { get; set; } = null!;
    }
}
