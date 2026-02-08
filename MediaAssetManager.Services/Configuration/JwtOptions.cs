namespace MediaAssetManager.Services.Configuration;

/// <summary>
/// Strongly-typed configuration for JWT authentication
/// Maps to "Jwt" section in appsettings.json
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key used for signing JWT tokens (minimum 32 characters)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer (typically your API domain)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT audience (typically your API consumers)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes (default: 60)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
