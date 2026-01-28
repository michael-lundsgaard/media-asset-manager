using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.Infrastructure.Extensions
{
    public static class MediaAssetQueryExtensions
    {
        public static IQueryable<MediaAsset> ApplyFilters(
            this IQueryable<MediaAsset> source,
            MediaAssetQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.FileName))
                source = source.Where(x => x.FileName.Contains(query.FileName));

            if (!string.IsNullOrWhiteSpace(query.Title))
                source = source.Where(x => x.Title != null && x.Title.Contains(query.Title));

            if (query.MinFileSizeBytes.HasValue)
                source = source.Where(x => x.FileSizeBytes >= query.MinFileSizeBytes.Value);

            if (query.MaxFileSizeBytes.HasValue)
                source = source.Where(x => x.FileSizeBytes <= query.MaxFileSizeBytes.Value);

            if (query.UploadedAfter.HasValue)
                source = source.Where(x => x.UploadedAt >= query.UploadedAfter.Value);

            if (query.UploadedBefore.HasValue)
                source = source.Where(x => x.UploadedAt <= query.UploadedBefore.Value);

            return source;
        }

        public static IQueryable<MediaAsset> ApplySorting(
            this IQueryable<MediaAsset> source,
            MediaAssetQuery query)
        {
            return query.SortBy switch
            {
                MediaAssetSortBy.FileName =>
                    query.SortDescending
                        ? source.OrderByDescending(x => x.FileName)
                        : source.OrderBy(x => x.FileName),

                MediaAssetSortBy.Title =>
                    query.SortDescending
                        ? source.OrderByDescending(x => x.Title)
                        : source.OrderBy(x => x.Title),

                MediaAssetSortBy.FileSizeBytes =>
                    query.SortDescending
                        ? source.OrderByDescending(x => x.FileSizeBytes)
                        : source.OrderBy(x => x.FileSizeBytes),

                _ =>
                    query.SortDescending
                        ? source.OrderByDescending(x => x.UploadedAt)
                        : source.OrderBy(x => x.UploadedAt)
            };
        }

        public static IQueryable<MediaAsset> ApplyPaging(
            this IQueryable<MediaAsset> source,
            MediaAssetQuery query)
        {
            return source
                .Skip(query.Skip)
                .Take(query.Take);
        }
    }

}
