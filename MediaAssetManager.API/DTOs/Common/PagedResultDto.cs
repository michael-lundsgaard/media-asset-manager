namespace MediaAssetManager.API.DTOs.Common
{
    /// <summary>
    /// Generic paged result response DTO
    /// </summary>
    /// <typeparam name="T">Type of items in the result</typeparam>
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public int PageCount => Take > 0 ? (int)Math.Ceiling((double)TotalCount / Take) : 0;
        public int CurrentPage => Take > 0 ? (Skip / Take) + 1 : 1;
        public bool HasNextPage => Skip + Take < TotalCount;
        public bool HasPreviousPage => Skip > 0;
    }
}
