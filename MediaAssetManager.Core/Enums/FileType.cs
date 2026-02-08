namespace MediaAssetManager.Core.Enums
{
    /// <summary>
    /// Type of file associated with a media asset
    /// </summary>
    public enum FileType
    {
        Original = 1,       // Original uploaded file
        Compressed = 2,     // Compressed/optimized version
        Thumbnail = 3,      // Thumbnail image
        PreviewGif = 4      // Animated preview (optional)
    }
}
