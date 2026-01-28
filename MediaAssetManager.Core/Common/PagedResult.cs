namespace MediaAssetManager.Core.Common
{
    /// <summary>
    /// Represents a single page of results from a larger, paged data set.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the paged result.</typeparam>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int TotalCount { get; }
        public int Skip { get; }
        public int Take { get; }

        public PagedResult(
            IReadOnlyList<T> items,
            int totalCount,
            int skip,
            int take)
        {
            Items = items;
            TotalCount = totalCount;
            Skip = skip;
            Take = take;
        }
    }
}
