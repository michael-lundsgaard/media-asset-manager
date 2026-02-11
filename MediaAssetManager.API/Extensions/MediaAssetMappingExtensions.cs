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
                PageSize = dto.PageSize,
                Expand = dto.Expand?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Converts MediaAsset entity to MediaAssetResponse DTO without expanded properties.
        /// Use the overload with expand parameter for conditional navigation property inclusion.
        /// </summary>
        public static MediaAssetResponse ToResponse(this MediaAsset entity)
        {
            return entity.ToResponse(null);
        }

        /// <summary>
        /// Converts MediaAsset entity to MediaAssetResponse DTO with conditional navigation property expansion.
        /// </summary>
        /// <param name="entity">The media asset entity to convert.</param>
        /// <param name="expand">Optional set of property names to expand (e.g., "user", "videoMetadata").</param>
        public static MediaAssetResponse ToResponse(this MediaAsset entity, HashSet<string>? expand)
        {
            var response = new MediaAssetResponse
            {
                AssetId = entity.AssetId,
                FileName = entity.FileName,
                OriginalFileName = entity.OriginalFileName,
                FileSizeBytes = entity.FileSizeBytes,
                Title = entity.Title,
                UploadedAt = entity.UploadedAt,
                ViewCount = entity.ViewCount
            };

            // Conditionally populate navigation properties based on expand parameter
            if (expand != null)
            {
                if (expand.Contains("user") && entity.User != null)
                {
                    response.User = entity.User.ToSummaryResponse();
                }

                if (expand.Contains("videoMetadata") && entity.VideoMetadata != null)
                {
                    response.VideoMetadata = entity.VideoMetadata.ToResponse();
                }
            }

            return response;
        }

        /// <summary>
        /// Converts User entity to UserSummaryResponse DTO (lightweight, no navigation properties).
        /// </summary>
        public static UserSummaryResponse ToSummaryResponse(this User entity)
        {
            return new UserSummaryResponse
            {
                UserId = entity.UserId,
                Username = entity.Username
            };
        }

        /// <summary>
        /// Converts VideoMetadata entity to VideoMetadataResponse DTO.
        /// </summary>
        public static VideoMetadataResponse ToResponse(this VideoMetadata entity)
        {
            return new VideoMetadataResponse
            {
                VideoMetadataId = entity.VideoMetadataId,
                AssetId = entity.AssetId,
                DurationSeconds = entity.DurationSeconds,
                Width = entity.Width,
                Height = entity.Height,
                FrameRate = entity.FrameRate,
                Codec = entity.Codec,
                BitrateKbps = entity.BitrateKbps,
                AudioCodec = entity.AudioCodec
            };
        }

        /// <summary>
        /// Converts MediaAsset entity to MediaAssetSummaryResponse DTO (for use in nested responses like playlist items).
        /// </summary>
        public static MediaAssetSummaryResponse ToSummaryResponse(this MediaAsset entity)
        {
            return new MediaAssetSummaryResponse
            {
                AssetId = entity.AssetId,
                FileName = entity.FileName,
                Title = entity.Title,
                FileSizeBytes = entity.FileSizeBytes,
                UploadedAt = entity.UploadedAt,
                ViewCount = entity.ViewCount
            };
        }

        /// <summary>
        /// Converts PagedResult of MediaAsset entities to PaginatedResponse of MediaAssetResponse DTOs.
        /// </summary>
        public static PaginatedResponse<MediaAssetResponse> ToPaginatedResponse(
            this PagedResult<MediaAsset> pagedResult,
            HashSet<string>? expand = null)
        {
            return new PaginatedResponse<MediaAssetResponse>
            {
                Items = pagedResult.Items.Select(x => x.ToResponse(expand)).ToList(),
                TotalCount = pagedResult.TotalCount,
                Page = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalPages = (int)Math.Ceiling(pagedResult.TotalCount / (double)pagedResult.PageSize),
            };
        }
    }
}
