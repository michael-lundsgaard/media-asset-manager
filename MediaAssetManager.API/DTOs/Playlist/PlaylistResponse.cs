using MediaAssetManager.API.DTOs.Common;

namespace MediaAssetManager.API.DTOs.Playlist
{
    /// <summary>
    /// Response DTO for playlist information.
    /// Supports conditional expansion of navigation properties via query parameters.
    /// </summary>
    public class PlaylistResponse
    {
        public int PlaylistId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Number of items in this playlist.
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// User who created the playlist. Populated when ?expand=user is specified.
        /// </summary>
        public UserSummaryResponse? User { get; set; }

        /// <summary>
        /// Items in this playlist. Populated when ?expand=items is specified.
        /// </summary>
        public ICollection<PlaylistItemResponse>? Items { get; set; }
    }
}
