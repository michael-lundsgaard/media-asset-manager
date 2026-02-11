using MediaAssetManager.Core.Common;
using MediaAssetManager.Core.Entities;
using MediaAssetManager.Core.Interfaces;
using MediaAssetManager.Core.Queries;
using MediaAssetManager.Services.Interfaces;

namespace MediaAssetManager.Services
{
    /// <summary>
    /// Service for media asset business logic operations
    /// </summary>
    public class MediaAssetService(IMediaAssetRepository repository) : IMediaAssetService
    {
        /// <inheritdoc/>
        public Task<PagedResult<MediaAsset>> GetAsync(MediaAssetQuery query)
        {
            // Pass query directly to repository - Expand is part of query
            return repository.GetAsync(query);
        }

        /// <inheritdoc/>
        public Task<MediaAsset?> GetByIdAsync(int id, HashSet<string>? expand = null)
        {
            // Simple pass-through for reads
            return repository.GetByIdAsync(id, expand);
        }

        /// <inheritdoc/>
        public async Task<MediaAsset> CreateAsync(MediaAsset asset, int userId)
        {
            // === BUSINESS VALIDATION ===
            if (string.IsNullOrWhiteSpace(asset.FileName))
                throw new ArgumentException("File name is required.", nameof(asset.FileName));

            if (string.IsNullOrWhiteSpace(asset.Title))
                throw new ArgumentException("Title is required.", nameof(asset.Title));

            if (asset.Title.Length > 200)
                throw new ArgumentException("Title cannot exceed 200 characters.", nameof(asset.Title));

            if (asset.Description?.Length > 2000)
                throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(asset.Description));

            if (asset.FileSizeBytes <= 0)
                throw new ArgumentException("File size must be greater than zero.", nameof(asset.FileSizeBytes));

            // === CHECK FOR DUPLICATES ===
            if (!string.IsNullOrWhiteSpace(asset.ContentHash))
            {
                var duplicate = await repository.GetByContentHashAsync(asset.ContentHash);
                if (duplicate != null)
                    throw new InvalidOperationException($"A file with this content already exists (AssetId: {duplicate.AssetId}).");
            }

            // === SET OWNERSHIP ===
            asset.UserId = userId; // Ensure asset is owned by the requesting user

            // === DELEGATE TO REPOSITORY ===
            return await repository.AddAsync(asset);
        }

        /// <inheritdoc/>
        public async Task<MediaAsset?> UpdateAsync(MediaAsset asset, int userId)
        {
            // === AUTHORIZATION CHECK ===
            var existing = await repository.GetByIdAsync(asset.AssetId);
            if (existing == null)
                return null;

            if (existing.UserId != userId)
                throw new UnauthorizedAccessException("You can only update your own media assets.");

            // === BUSINESS VALIDATION ===
            if (string.IsNullOrWhiteSpace(asset.Title))
                throw new ArgumentException("Title is required.", nameof(asset.Title));

            if (asset.Title.Length > 200)
                throw new ArgumentException("Title cannot exceed 200 characters.", nameof(asset.Title));

            if (asset.Description?.Length > 2000)
                throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(asset.Description));

            // === PRESERVE CRITICAL FIELDS ===
            asset.UserId = existing.UserId; // Prevent ownership changes
            asset.ContentHash = existing.ContentHash; // Prevent hash changes
            asset.UploadedAt = existing.UploadedAt; // Prevent timestamp manipulation

            // === DELEGATE TO REPOSITORY ===
            return await repository.UpdateAsync(asset);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id, int userId)
        {
            // === AUTHORIZATION CHECK ===
            var existing = await repository.GetByIdAsync(id);
            if (existing == null)
                return false;

            if (existing.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own media assets.");

            // TODO: Add storage cleanup (delete from B2) - orchestrate with IStorageService
            // await storageService.DeleteFileAsync(existing.StoragePath);
            // if (existing.ThumbnailPath != null)
            //     await storageService.DeleteFileAsync(existing.ThumbnailPath);

            // === DELEGATE TO REPOSITORY ===
            return await repository.DeleteAsync(id);
        }
    }
}
