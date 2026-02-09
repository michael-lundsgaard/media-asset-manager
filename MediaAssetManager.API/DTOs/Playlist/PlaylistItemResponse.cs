using MediaAssetManager.API.DTOs.Common;

namespace MediaAssetManager.API.DTOs.Playlist
{
    /// <summary>
    /// Response DTO for a playlist item (asset in a playlist).
    /// Uses MediaAssetSummaryResponse to prevent circular references.
    /// </summary>
    public class PlaylistItemResponse
    {
        public int PlaylistItemId { get; set; }
        public int PlaylistId { get; set; }
        public DateTime AddedAt { get; set; }

        /// <summary>
        /// Summary information about the media asset in this playlist.
        /// Uses lightweight DTO to avoid circular references and excessive nesting.
        /// </summary>
        public MediaAssetSummaryResponse Asset { get; set; } = null!;
    }
}
