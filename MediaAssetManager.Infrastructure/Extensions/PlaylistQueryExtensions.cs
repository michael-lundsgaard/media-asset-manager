using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Infrastructure.Extensions
{
    public static class PlaylistQueryExtensions
    {
        /// <summary>
        /// Applies filtering based on the provided PlaylistQuery.
        /// </summary>
        public static IQueryable<Playlist> ApplyFilters(
            this IQueryable<Playlist> source,
            PlaylistQuery query)
        {
            if (query.UserId.HasValue)
                source = source.Where(x => x.UserId == query.UserId.Value);

            if (query.IsPublic.HasValue)
                source = source.Where(x => x.IsPublic == query.IsPublic.Value);

            if (!string.IsNullOrWhiteSpace(query.Name))
                source = source.Where(x => x.Name.Contains(query.Name));

            return source;
        }

        /// <summary>
        /// Applies sorting based on the provided PlaylistQuery.
        /// </summary>
        public static IQueryable<Playlist> ApplySorting(
            this IQueryable<Playlist> source,
            PlaylistQuery query)
        {
            return query.SortBy switch
            {
                PlaylistSortBy.Name =>
                    query.SortDescending
                        ? source.OrderByDescending(x => x.Name)
                        : source.OrderBy(x => x.Name),

                PlaylistSortBy.IsPublic =>
                    query.SortDescending
                        ? source.OrderByDescending(x => x.IsPublic)
                        : source.OrderBy(x => x.IsPublic),

                _ =>
                    query.SortDescending
                        ? source.OrderByDescending(x => x.CreatedAt)
                        : source.OrderBy(x => x.CreatedAt)
            };
        }

        /// <summary>
        /// Applies paging based on the provided PlaylistQuery.
        /// </summary>
        public static IQueryable<Playlist> ApplyPaging(
            this IQueryable<Playlist> source,
            PlaylistQuery query)
        {
            return source
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize);
        }
    }
}
