namespace MediaAssetManager.Core.Enums
{
    public enum ProcessingStatus
    {
        Pending = 1,        // Just uploaded, awaiting processing
        Processing = 2,     // Currently being processed
        Completed = 3,      // Successfully processed and ready
        Failed = 4          // Processing failed
    }
}
