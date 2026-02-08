namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// User favorites/likes for media assets
    /// </summary>
    public class Favorite
    {
        public int FavoriteId { get; set; }
        public int UserId { get; set; }
        public int AssetId { get; set; }
        public DateTime CreatedAt { get; set; }

        // === NAVIGATION PROPERTIES ===
        public User User { get; set; } = null!;
        public MediaAsset Asset { get; set; } = null!;
    }
}
