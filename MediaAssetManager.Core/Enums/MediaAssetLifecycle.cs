namespace MediaAssetManager.Core.Enums
{
    public enum MediaAssetLifecycle
    {
        Active,        // Normal, visible, usable
        Orphaned,      // User deleted, asset retained
        Archived,      // Hidden but preserved
        PendingDelete, // Marked for cleanup
        Deleted        // Logically deleted (file may or may not be gone)
    }
}