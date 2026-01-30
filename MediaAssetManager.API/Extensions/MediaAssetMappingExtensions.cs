using MediaAssetManager.API.DTOs;
using MediaAssetManager.API.DTOs.Common;
using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Queries;

namespace MediaAssetManager.API.Extensions
{
    public static class MediaAssetMappingExtensions
    {
        /// <summary>
        /// Converts MediaAssetQueryRequest DTO to MediaAssetQuery.
        /// </summary>
        public static MediaAssetQuery ToQuery(this MediaAssetQueryRequest dto)
        {
            return new MediaAssetQuery
            {
                FileName = dto.FileName,
                Title = dto.Title,
                MinFileSizeBytes = dto.MinFileSizeBytes,
                MaxFileSizeBytes = dto.MaxFileSizeBytes,
                UploadedAfter = dto.UploadedAfter,
                UploadedBefore = dto.UploadedBefore,
                SortBy = dto.SortBy,
                SortDescending = dto.SortDescending,
                PageNumber = dto.Page,
                PageSize = dto.PageSize
            };
        }

        /// <summary>
        /// Converts MediaAsset entity to MediaAssetResponse DTO.
        /// </summary>
        public static MediaAssetResponse ToResponse(this MediaAsset entity)
        {
            return new MediaAssetResponse
            {
                AssetId = entity.AssetId,
                FileName = entity.FileName,
                OriginalFileName = entity.OriginalFileName,
                FileSizeBytes = entity.FileSizeBytes,
                Title = entity.Title,
                UploadedAt = entity.UploadedAt
            };
        }

        /// <summary>
        /// Converts PagedResult of MediaAsset entities to PaginatedResponse of MediaAssetResponse DTOs.
        /// </summary>
        public static PaginatedResponse<MediaAssetResponse> ToPaginatedResponse(this PagedResult<MediaAsset> pagedResult)
        {
            return new PaginatedResponse<MediaAssetResponse>
            {
                Items = pagedResult.Items.Select(x => x.ToResponse()).ToList(),
                TotalCount = pagedResult.TotalCount,
                Page = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalPages = (int)Math.Ceiling(pagedResult.TotalCount / (double)pagedResult.PageSize),
            };
        }
    }
}
