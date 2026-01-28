using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using MediaAssetManager.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaAssetManager.Services
{
    public class B2StorageService(IConfiguration configuration, ILogger<B2StorageService> logger) : IStorageService
    {
        private readonly string _bucketName = configuration["B2:BucketName"] ?? throw new ArgumentNullException("B2:BucketName");

        private readonly AmazonS3Client _s3Client = new(
            new BasicAWSCredentials(
                configuration["B2:KeyId"] ?? throw new ArgumentNullException("B2:KeyId"),
                configuration["B2:KeySecret"] ?? throw new ArgumentNullException("B2:KeySecret")
            ),
            new AmazonS3Config
            {
                ServiceURL = configuration["B2:Endpoint"] ?? throw new ArgumentNullException("B2:Endpoint"),
                ForcePathStyle = true
            }
        );

        /// <inheritdoc/>
        public async Task<(string FileId, string FileName, long FileSize)> UploadFileAsync(Stream fileStream, string originalFileName, string contentType)
        {
            logger.LogInformation("Uploading file {FileName} to bucket {BucketName}", originalFileName, _bucketName);
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = originalFileName,
                    InputStream = fileStream,
                    ContentType = contentType
                };

                var response = await _s3Client.PutObjectAsync(request);

                logger.LogInformation("File {FileName} uploaded successfully. ETag: {ETag}", originalFileName, response.ETag);

                return (response.ETag, originalFileName, fileStream.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload file {FileName}", originalFileName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetSignedDownloadUrlAsync(string fileName, int expirationSeconds = 3600)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    Expires = DateTime.UtcNow.AddSeconds(expirationSeconds)
                };

                var url = await _s3Client.GetPreSignedURLAsync(request);

                logger.LogInformation("Generated signed URL for {FileName} valid for {Seconds} seconds", fileName, expirationSeconds);

                return url;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate signed URL for {FileName}", fileName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFileAsync(string fileId, string fileName)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName
                };

                await _s3Client.DeleteObjectAsync(request);

                logger.LogInformation("File {FileName} deleted successfully", fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete file {FileName}", fileName);
                throw;
            }
        }
    }
}
