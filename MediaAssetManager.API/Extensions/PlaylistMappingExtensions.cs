using MediaAssetManager.API.DTOs.Common;
using MediaAssetManager.API.DTOs.Playlist;
using MediaAssetManager.Core.Entities;

namespace MediaAssetManager.API.Extensions
{
    /// <summary>
    /// Extension methods for mapping Playlist entities to DTOs.
    /// </summary>
    public static class PlaylistMappingExtensions
    {
        /// <summary>
        /// Converts Playlist entity to PlaylistResponse DTO without expanded properties.
        /// Use the overload with expand parameter for conditional navigation property inclusion.
        /// </summary>
        public static PlaylistResponse ToResponse(this Core.Entities.Playlist entity)
        {
            return entity.ToResponse(null);
        }

        /// <summary>
        /// Converts Playlist entity to PlaylistResponse DTO with conditional navigation property expansion.
        /// </summary>
        /// <param name="entity">The playlist entity to convert.</param>
        /// <param name="expand">Optional set of property names to expand (e.g., "user", "items").</param>
        public static PlaylistResponse ToResponse(this Core.Entities.Playlist entity, HashSet<string>? expand)
        {
            var response = new PlaylistResponse
            {
                PlaylistId = entity.PlaylistId,
                Name = entity.Name,
                Description = entity.Description,
                IsPublic = entity.IsPublic,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ItemCount = entity.Items?.Count ?? 0
            };

            // Conditionally populate navigation properties based on expand parameter
            if (expand != null)
            {
                if (expand.Contains("user") && entity.User != null)
                {
                    response.User = entity.User.ToSummaryResponse();
                }

                if (expand.Contains("items") && entity.Items != null)
                {
                    response.Items = entity.Items
                        .Select(item => item.ToResponse())
                        .ToList();
                }
            }

            return response;
        }

        /// <summary>
        /// Converts PlaylistItem entity to PlaylistItemResponse DTO.
        /// </summary>
        public static PlaylistItemResponse ToResponse(this PlaylistItem entity)
        {
            return new PlaylistItemResponse
            {
                PlaylistItemId = entity.PlaylistItemId,
                PlaylistId = entity.PlaylistId,
                AddedAt = entity.AddedAt,
                Asset = entity.Asset.ToSummaryResponse()
            };
        }

        /// <summary>
        /// Converts Playlist entity to PlaylistSummaryResponse DTO (for use in nested responses).
        /// </summary>
        public static PlaylistSummaryResponse ToSummaryResponse(this Core.Entities.Playlist entity)
        {
            return new PlaylistSummaryResponse
            {
                PlaylistId = entity.PlaylistId,
                Name = entity.Name,
                Description = entity.Description,
                IsPublic = entity.IsPublic,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
