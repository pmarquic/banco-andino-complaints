using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using BancoAndino.Complaints.Shared.Models;
using BancoAndino.Complaints.Shared.DTOs;
using BancoAndino.Complaints.Shared.Interfaces;

namespace BancoAndino.Complaints.API.Functions;

public class ComplaintsFunctions
{
    private readonly ILogger<ComplaintsFunctions> _logger;
    private readonly IComplaintRepository _repository;
    private readonly ICacheService _cacheService;

    public ComplaintsFunctions(ILogger<ComplaintsFunctions> logger, IComplaintRepository repository, ICacheService cacheService)
    {
        _logger = logger;
        _repository = repository;
        _cacheService = cacheService;
    }

    [Function("GetComplaints")]
    public async Task<HttpResponseData> GetComplaints(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "complaints")] HttpRequestData req)
    {
        _logger.LogInformation("Getting all complaints");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var statusId = query["statusId"] != null ? int.Parse(query["statusId"]!) : (int?)null;
            var categoryId = query["categoryId"] != null ? int.Parse(query["categoryId"]!) : (int?)null;
            var customerId = query["customerId"];

            var complaints = await _repository.GetAllAsync(statusId, categoryId, customerId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(complaints);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting complaints");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("GetComplaintById")]
    public async Task<HttpResponseData> GetComplaintById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "complaints/{id}")] HttpRequestData req,
        int id)
    {
        _logger.LogInformation("Getting complaint {ComplaintId}", id);

        try
        {
            // Try cache first
            var cacheKey = $"complaint:{id}";
            var cached = await _cacheService.GetAsync<Complaint>(cacheKey);
            if (cached != null)
            {
                var cachedResponse = req.CreateResponse(HttpStatusCode.OK);
                await cachedResponse.WriteAsJsonAsync(cached);
                return cachedResponse;
            }

            var complaint = await _repository.GetByIdAsync(id);
            if (complaint == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Complaint {id} not found");
                return notFoundResponse;
            }

            // Cache for 5 minutes
            await _cacheService.SetAsync(cacheKey, complaint, TimeSpan.FromMinutes(5));

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(complaint);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting complaint {ComplaintId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("CreateComplaint")]
    public async Task<HttpResponseData> CreateComplaint(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "complaints")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new complaint");

        try
        {
            var dto = await req.ReadFromJsonAsync<CreateComplaintDto>();
            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid complaint data");
                return badRequest;
            }

            var complaint = new Complaint
            {
                CustomerId = dto.CustomerId,
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                Priority = dto.Priority,
                StatusId = 1, // New status
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Calculate SLA deadline based on category
            // This would typically fetch category info, but for simplicity using default
            complaint.SlaDeadline = DateTime.UtcNow.AddHours(48);

            var created = await _repository.CreateAsync(complaint);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(created);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating complaint");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("UpdateComplaint")]
    public async Task<HttpResponseData> UpdateComplaint(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "complaints/{id}")] HttpRequestData req,
        int id)
    {
        _logger.LogInformation("Updating complaint {ComplaintId}", id);

        try
        {
            var dto = await req.ReadFromJsonAsync<UpdateComplaintDto>();
            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid complaint data");
                return badRequest;
            }

            var complaint = await _repository.GetByIdAsync(id);
            if (complaint == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Complaint {id} not found");
                return notFoundResponse;
            }

            // Update only provided fields
            if (dto.Title != null) complaint.Title = dto.Title;
            if (dto.Description != null) complaint.Description = dto.Description;
            if (dto.CategoryId.HasValue) complaint.CategoryId = dto.CategoryId.Value;
            if (dto.Priority != null) complaint.Priority = dto.Priority;
            if (dto.AssignedTo != null) complaint.AssignedTo = dto.AssignedTo;

            var updated = await _repository.UpdateAsync(complaint);

            // Invalidate cache
            await _cacheService.RemoveAsync($"complaint:{id}");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(updated);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating complaint {ComplaintId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("UpdateComplaintStatus")]
    public async Task<HttpResponseData> UpdateComplaintStatus(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "complaints/{id}/status")] HttpRequestData req,
        int id)
    {
        _logger.LogInformation("Updating status for complaint {ComplaintId}", id);

        try
        {
            var dto = await req.ReadFromJsonAsync<UpdateStatusDto>();
            if (dto == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid status data");
                return badRequest;
            }

            var success = await _repository.UpdateStatusAsync(id, dto.StatusId, dto.ChangedBy, dto.Notes);
            if (!success)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Complaint {id} not found");
                return notFoundResponse;
            }

            // Invalidate cache
            await _cacheService.RemoveAsync($"complaint:{id}");

            var complaint = await _repository.GetByIdAsync(id);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(complaint);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for complaint {ComplaintId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("DeleteComplaint")]
    public async Task<HttpResponseData> DeleteComplaint(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "complaints/{id}")] HttpRequestData req,
        int id)
    {
        _logger.LogInformation("Deleting complaint {ComplaintId}", id);

        try
        {
            var success = await _repository.DeleteAsync(id);
            if (!success)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Complaint {id} not found");
                return notFoundResponse;
            }

            // Invalidate cache
            await _cacheService.RemoveAsync($"complaint:{id}");

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting complaint {ComplaintId}", id);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
