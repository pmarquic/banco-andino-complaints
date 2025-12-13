using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using BancoAndino.Complaints.Shared.Interfaces;

namespace BancoAndino.Complaints.API.Services;

public class StorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<StorageService>? _logger;

    public StorageService(string connectionString, ILogger<StorageService>? logger = null)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string container = "evidences")
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders
        });

        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteFileAsync(string blobUrl)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            await blobClient.DeleteIfExistsAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting blob: {BlobUrl}", blobUrl);
            return false;
        }
    }

    public async Task<Stream> DownloadFileAsync(string blobUrl)
    {
        var blobClient = new BlobClient(new Uri(blobUrl));
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    public async Task<bool> FileExistsAsync(string blobUrl)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking blob existence: {BlobUrl}", blobUrl);
            return false;
        }
    }
}
