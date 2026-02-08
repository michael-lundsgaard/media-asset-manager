using MediaAssetManager.Core.Entities;

namespace MediaAssetManager.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for analytics data access operations.
    /// </summary>
    public interface IAnalyticsRepository
    {
        /// <summary>
        /// Tracks a view event for a media asset.
        /// </summary>
        /// <param name="view">The view event to track.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task TrackViewAsync(AssetView view);

        /// <summary>
        /// Gets view count for a specific asset within a date range.
        /// </summary>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the view count.</returns>
        Task<int> GetViewCountAsync(int assetId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets daily view counts for a specific asset within a date range.
        /// </summary>
        /// <param name="assetId">The unique identifier of the media asset.</param>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of date to view count.</returns>
        Task<Dictionary<DateTime, int>> GetDailyViewsAsync(int assetId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets daily upload counts for a specific user within a date range (for heatmap).
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of date to upload count.</returns>
        Task<Dictionary<DateTime, int>> GetUserUploadHeatmapAsync(int userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets most viewed assets within a date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <param name="topCount">The number of top assets to return.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of tuples with asset ID and view count.</returns>
        Task<List<(int AssetId, int ViewCount)>> GetTopViewedAssetsAsync(DateTime startDate, DateTime endDate, int topCount = 10);
    }
}
