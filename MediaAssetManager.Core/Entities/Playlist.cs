namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// User-created playlist for organizing media assets
    /// </summary>
    public class Playlist
    {
        public int PlaylistId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        // === NAVIGATION PROPERTIES ===
        public User User { get; set; } = null!;
        public ICollection<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
    }
}
