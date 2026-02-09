namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// Lightweight user information for nested responses.
    /// Does NOT include navigation properties to prevent circular references.
    /// </summary>
    public class UserSummaryResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}
