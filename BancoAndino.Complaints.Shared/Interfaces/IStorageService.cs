namespace BancoAndino.Complaints.Shared.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string container = "evidences");
    Task<bool> DeleteFileAsync(string blobUrl);
    Task<Stream> DownloadFileAsync(string blobUrl);
    Task<bool> FileExistsAsync(string blobUrl);
}
