using MediaAssetManager.Core.Enums;

namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// User entity - represents an application user.
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        // Navigation properties for resources
        public ICollection<MediaAsset> Assets { get; set; } = [];
        public ICollection<Playlist> Playlists { get; set; } = [];
        public ICollection<Favorite> Favorites { get; set; } = [];
    }
}
