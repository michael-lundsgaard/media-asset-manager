namespace MediaAssetManager.Core.Enums
{
    public enum MediaAssetLifecycle
    {
        Active = 1,        // Normal, visible, usable
        Orphaned = 2,      // User deleted, asset retained
        Archived = 3,      // Hidden but preserved
        PendingDelete = 4, // Marked for cleanup
        Deleted = 5        // Logically deleted (file may or may not be gone)
    }
}