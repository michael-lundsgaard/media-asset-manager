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
        /// Converts MediaAssetQueryDto to MediaAssetQuery domain object
        /// </summary>
        public static MediaAssetQuery ToQuery(this MediaAssetQueryDto dto)
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
                Skip = dto.Skip,
                Take = dto.Take
            };
        }

        /// <summary>
        /// Converts MediaAsset entity to MediaAssetResponseDto
        /// </summary>
        public static MediaAssetResponseDto ToDto(this MediaAsset entity)
        {
            return new MediaAssetResponseDto
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
        /// Converts PagedResult of MediaAsset entities to PagedResultDto of MediaAssetResponseDto
        /// </summary>
        public static PagedResultDto<MediaAssetResponseDto> ToDto(this PagedResult<MediaAsset> pagedResult)
        {
            return new PagedResultDto<MediaAssetResponseDto>
            {
                Items = pagedResult.Items.Select(x => x.ToDto()).ToList(),
                TotalCount = pagedResult.TotalCount,
                Skip = pagedResult.Skip,
                Take = pagedResult.Take
            };
        }
    }
}
