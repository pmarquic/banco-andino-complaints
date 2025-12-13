using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BancoAndino.Complaints.EvidenceProcessor;

public class ProcessEvidenceFunction
{
    private readonly ILogger<ProcessEvidenceFunction> _logger;
    private readonly string _connectionString;
    private readonly BlobServiceClient _blobServiceClient;

    public ProcessEvidenceFunction(ILogger<ProcessEvidenceFunction> logger)
    {
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("SqlConnectionString") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=BancoAndinoComplaints;Trusted_Connection=True;";
        
        var blobConnectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString") 
            ?? "UseDevelopmentStorage=true";
        _blobServiceClient = new BlobServiceClient(blobConnectionString);
    }

    [Function("ProcessEvidence")]
    public async Task ProcessEvidence(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation("Event Grid trigger function processed a request.");
        _logger.LogInformation("Event type: {EventType}, Event subject: {EventSubject}", 
            eventGridEvent.EventType, eventGridEvent.Subject);

        try
        {
            // Parse the event data
            var data = JsonSerializer.Deserialize<BlobCreatedEventData>(eventGridEvent.Data.ToString()!);
            if (data == null)
            {
                _logger.LogWarning("Unable to parse event data");
                return;
            }

            var blobUrl = data.Url;
            _logger.LogInformation("Processing blob: {BlobUrl}", blobUrl);

            // Extract blob metadata
            var blobClient = new BlobClient(new Uri(blobUrl));
            var properties = await blobClient.GetPropertiesAsync();

            // Validate file type (example: only allow PDF, JPG, PNG)
            var allowedContentTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
            if (!allowedContentTypes.Contains(properties.Value.ContentType))
            {
                _logger.LogWarning("Invalid file type: {ContentType}", properties.Value.ContentType);
                // Optionally delete the blob
                await blobClient.DeleteIfExistsAsync();
                return;
            }

            // Validate file size (example: max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (properties.Value.ContentLength > maxFileSize)
            {
                _logger.LogWarning("File too large: {FileSize} bytes", properties.Value.ContentLength);
                await blobClient.DeleteIfExistsAsync();
                return;
            }

            // Update evidence metadata in database if needed
            await UpdateEvidenceMetadata(blobUrl, properties.Value.ContentLength, properties.Value.ContentType);

            _logger.LogInformation("Successfully processed evidence: {BlobUrl}", blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing evidence from Event Grid");
            throw;
        }
    }

    private async Task UpdateEvidenceMetadata(string blobUrl, long fileSize, string contentType)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            UPDATE Evidences 
            SET FileSize = @FileSize, ContentType = @ContentType 
            WHERE BlobUrl = @BlobUrl";

        await connection.ExecuteAsync(sql, new { BlobUrl = blobUrl, FileSize = fileSize, ContentType = contentType });
        
        _logger.LogInformation("Updated evidence metadata for {BlobUrl}", blobUrl);
    }
}

// Event Grid event data model for blob created event
public class BlobCreatedEventData
{
    public string Api { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string ETag { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string BlobType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Sequencer { get; set; } = string.Empty;
    public Dictionary<string, string> StorageDiagnostics { get; set; } = new();
}
