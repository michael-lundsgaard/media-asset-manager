namespace MediaAssetManager.Services.Configuration;

/// <summary>
/// Strongly-typed configuration for Backblaze B2 storage
/// Maps to "B2" section in appsettings.json
/// </summary>
public class B2StorageOptions
{
    public const string SectionName = "B2";

    /// <summary>
    /// Backblaze B2 Application Key ID
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Backblaze B2 Application Key Secret
    /// </summary>
    public string KeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Name of the B2 bucket to store media files
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// S3-compatible endpoint URL for Backblaze B2
    /// Example: https://s3.eu-central-003.backblazeb2.com
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
}
