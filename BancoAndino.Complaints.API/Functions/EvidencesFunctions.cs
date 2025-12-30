using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using BancoAndino.Complaints.Shared.Models;
using BancoAndino.Complaints.Shared.Interfaces;
using BancoAndino.Complaints.Shared.Data;

namespace BancoAndino.Complaints.API.Functions;

public class EvidencesFunctions
{
    private readonly ILogger<EvidencesFunctions> _logger;
    private readonly IComplaintRepository _repository;
    private readonly IStorageService _storageService;
    private readonly ComplaintsDbContext _context;

    public EvidencesFunctions(
        ILogger<EvidencesFunctions> logger, 
        IComplaintRepository repository, 
        IStorageService storageService,
        ComplaintsDbContext context)
    {
        _logger = logger;
        _repository = repository;
        _storageService = storageService;
        _context = context;
    }

    [Function("GetEvidences")]
    public async Task<HttpResponseData> GetEvidences(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "complaints/{id}/evidences")] HttpRequestData req,
        int id)
    {
        _logger.LogInformation("Getting evidences for complaint {ComplaintId}", id);

        try
        {
            var evidences = await _repository.GetEvidencesAsync(id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(evidences);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evidences for complaint {ComplaintId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("UploadEvidence")]
    public async Task<HttpResponseData> UploadEvidence(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "complaints/{id}/evidences")] HttpRequestData req,
        int id)
    {
        _logger.LogInformation("Uploading evidence for complaint {ComplaintId}", id);

        try
        {
            // Verify complaint exists
            var complaint = await _repository.GetByIdAsync(id);
            if (complaint == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Complaint {id} not found");
                return notFoundResponse;
            }

            // Parse multipart form data
            var boundary = GetBoundary(req.Headers.GetValues("Content-Type").FirstOrDefault());
            if (string.IsNullOrEmpty(boundary))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid content type");
                return badRequest;
            }

            // For simplicity, we'll create a mock evidence entry
            // In production, you would parse the multipart form data properly
            var fileName = "evidence_" + Guid.NewGuid().ToString() + ".pdf";
            var contentType = "application/pdf";
            
            // Create a memory stream for demo purposes
            using var memoryStream = new MemoryStream();
            await req.Body.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var fileSize = memoryStream.Length;

            // Upload to blob storage
            var blobUrl = await _storageService.UploadFileAsync(memoryStream, fileName, contentType);

            // Save evidence record
            var evidence = new Evidence
            {
                ComplaintId = id,
                BlobUrl = blobUrl,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = "system" // Should come from authentication
            };

            _context.Evidences.Add(evidence);
            await _context.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(evidence);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading evidence for complaint {ComplaintId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    private static string? GetBoundary(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return null;

        var elements = contentType.Split(' ');
        var element = elements.FirstOrDefault(e => e.StartsWith("boundary="));
        if (element == null)
            return null;

        return element.Substring("boundary=".Length).Trim('"');
    }
}
