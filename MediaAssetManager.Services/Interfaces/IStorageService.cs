namespace MediaAssetManager.Services.Interfaces
{
    /// <summary>
    /// Provides cloud storage operations for media assets.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Uploads a file to the storage service.
        /// </summary>
        /// <param name="fileStream">The stream containing the file data.</param>
        /// <param name="originalFileName">The original name of the file.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <returns>A tuple containing the file ID, file name, and file size in bytes.</returns>
        Task<(string FileId, string FileName, long FileSize)> UploadFileAsync(Stream fileStream, string originalFileName, string contentType);

        /// <summary>
        /// Generates a pre-signed URL for downloading a file.
        /// </summary>
        /// <param name="fileName">The name of the file to generate a URL for.</param>
        /// <param name="expirationSeconds">The number of seconds until the URL expires. Default is 3600 (1 hour).</param>
        /// <returns>A pre-signed URL that can be used to download the file.</returns>
        Task<string> GetSignedDownloadUrlAsync(string fileName, int expirationSeconds = 3600);

        /// <summary>
        /// Deletes a file from the storage service.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file.</param>
        /// <param name="fileName">The name of the file to delete.</param>
        Task DeleteFileAsync(string fileId, string fileName);
    }
}
