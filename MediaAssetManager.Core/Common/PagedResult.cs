namespace MediaAssetManager.Core.Common
{

    /// <summary>
    /// Domain object representing a page of results.
    /// </summary>
    public class PagedResult<T>(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        public IReadOnlyList<T> Items { get; } = items;
        public int TotalCount { get; } = totalCount;
        public int PageNumber { get; } = pageNumber;
        public int PageSize { get; } = pageSize;

        // Domain helper methods
        public bool IsFirstPage => PageNumber == 1;
        public bool IsLastPage => PageNumber * PageSize >= TotalCount;

        public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            return new PagedResult<TResult>(
                Items.Select(mapper).ToList(),
                TotalCount,
                PageNumber,
                PageSize
            );
        }
    }
}
