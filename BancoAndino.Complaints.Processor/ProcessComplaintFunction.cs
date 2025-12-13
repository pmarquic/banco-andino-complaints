using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using BancoAndino.Complaints.Shared.Models;

namespace BancoAndino.Complaints.Processor;

public class ProcessComplaintFunction
{
    private readonly ILogger<ProcessComplaintFunction> _logger;
    private readonly string _connectionString;

    public ProcessComplaintFunction(ILogger<ProcessComplaintFunction> logger)
    {
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("SqlConnectionString") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=BancoAndinoComplaints;Trusted_Connection=True;";
    }

    [Function("ProcessComplaint")]
    public async Task ProcessComplaint(
        [ServiceBusTrigger("complaint-processing", Connection = "ServiceBusConnectionString")] 
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Service Bus queue trigger function processed message: {MessageId}", message.MessageId);

        try
        {
            // Deserialize the message body
            var complaint = JsonSerializer.Deserialize<ComplaintMessage>(message.Body.ToString());
            if (complaint == null)
            {
                _logger.LogWarning("Unable to deserialize complaint message");
                await messageActions.DeadLetterMessageAsync(message, new Dictionary<string, object>
                {
                    { "Reason", "Unable to deserialize message" }
                });
                return;
            }

            _logger.LogInformation("Processing complaint {ComplaintId}", complaint.ComplaintId);

            // Perform asynchronous processing
            await AssignComplaint(complaint.ComplaintId, complaint.CategoryId, complaint.Priority);
            
            // Send notifications (simulated)
            await SendNotification(complaint.ComplaintId, complaint.CustomerId);

            // Update complaint status to "In Progress"
            await UpdateComplaintStatus(complaint.ComplaintId, 2); // Status 2 = In Progress

            _logger.LogInformation("Successfully processed complaint {ComplaintId}", complaint.ComplaintId);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing complaint from Service Bus");
            
            // Optionally, dead-letter the message if it can't be processed
            try
            {
                await messageActions.DeadLetterMessageAsync(message, new Dictionary<string, object>
                {
                    { "ErrorMessage", ex.Message }
                });
            }
            catch (Exception deadLetterEx)
            {
                _logger.LogError(deadLetterEx, "Error dead-lettering message");
            }
        }
    }

    private async Task AssignComplaint(int complaintId, int categoryId, string priority)
    {
        using var connection = new SqlConnection(_connectionString);

        // Simple auto-assignment logic based on category and priority
        // In production, this would be more sophisticated (load balancing, skills matching, etc.)
        var assignedTo = categoryId switch
        {
            1 => "account-specialist@bancoandin.com",    // Account Issues
            2 => "card-specialist@bancoandino.com",      // Card Problems
            3 => "fraud-investigator@bancoandino.com",   // Transaction Dispute
            4 => "customer-service@bancoandino.com",     // Service Quality
            _ => "general-support@bancoandino.com"       // Other
        };

        // Update the complaint with assigned agent
        var sql = @"
            UPDATE Complaints 
            SET AssignedTo = @AssignedTo, UpdatedAt = GETUTCDATE() 
            WHERE ComplaintId = @ComplaintId";

        await connection.ExecuteAsync(sql, new { ComplaintId = complaintId, AssignedTo = assignedTo });
        
        _logger.LogInformation("Assigned complaint {ComplaintId} to {AssignedTo}", complaintId, assignedTo);
    }

    private async Task SendNotification(int complaintId, string customerId)
    {
        // Simulate sending notification (email, SMS, push notification)
        // In production, this would integrate with a notification service
        
        _logger.LogInformation("Sending notification for complaint {ComplaintId} to customer {CustomerId}", 
            complaintId, customerId);

        // Simulate async work
        await Task.Delay(100);
        
        _logger.LogInformation("Notification sent successfully");
    }

    private async Task UpdateComplaintStatus(int complaintId, int statusId)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            UPDATE Complaints 
            SET StatusId = @StatusId, UpdatedAt = GETUTCDATE() 
            WHERE ComplaintId = @ComplaintId";

        await connection.ExecuteAsync(sql, new { ComplaintId = complaintId, StatusId = statusId });

        // Add history record
        var historySql = @"
            INSERT INTO ComplaintHistories (ComplaintId, OldStatusId, NewStatusId, ChangedBy, Notes, ChangedAt)
            SELECT @ComplaintId, 1, @StatusId, 'System', 'Auto-processed by complaint processor', GETUTCDATE()";

        await connection.ExecuteAsync(historySql, new { ComplaintId = complaintId, StatusId = statusId });
        
        _logger.LogInformation("Updated complaint {ComplaintId} status to {StatusId}", complaintId, statusId);
    }
}

// Message model for Service Bus
public class ComplaintMessage
{
    public int ComplaintId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Priority { get; set; } = string.Empty;
}
