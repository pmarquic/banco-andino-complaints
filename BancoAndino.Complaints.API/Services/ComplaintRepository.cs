using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using BancoAndino.Complaints.Shared.Models;
using BancoAndino.Complaints.Shared.Interfaces;
using BancoAndino.Complaints.Shared.Data;

namespace BancoAndino.Complaints.API.Services;

public class ComplaintRepository : IComplaintRepository
{
    private readonly ComplaintsDbContext _context;
    private readonly string _connectionString;

    public ComplaintRepository(ComplaintsDbContext context, string connectionString)
    {
        _context = context;
        _connectionString = connectionString;
    }

    public async Task<Complaint?> GetByIdAsync(int complaintId)
    {
        return await _context.Complaints
            .Include(c => c.Status)
            .Include(c => c.Category)
            .Include(c => c.Evidences)
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
    }

    public async Task<IEnumerable<Complaint>> GetAllAsync(int? statusId = null, int? categoryId = null, string? customerId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            SELECT c.*, s.*, cat.*
            FROM Complaints c
            INNER JOIN ComplaintStatuses s ON c.StatusId = s.StatusId
            INNER JOIN ComplaintCategories cat ON c.CategoryId = cat.CategoryId
            WHERE (@StatusId IS NULL OR c.StatusId = @StatusId)
              AND (@CategoryId IS NULL OR c.CategoryId = @CategoryId)
              AND (@CustomerId IS NULL OR c.CustomerId = @CustomerId)
            ORDER BY c.CreatedAt DESC";

        var complaints = await connection.QueryAsync<Complaint, ComplaintStatus, ComplaintCategory, Complaint>(
            sql,
            (complaint, status, category) =>
            {
                complaint.Status = status;
                complaint.Category = category;
                return complaint;
            },
            new { StatusId = statusId, CategoryId = categoryId, CustomerId = customerId },
            splitOn: "StatusId,CategoryId"
        );

        return complaints;
    }

    public async Task<Complaint> CreateAsync(Complaint complaint)
    {
        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();
        return complaint;
    }

    public async Task<Complaint> UpdateAsync(Complaint complaint)
    {
        complaint.UpdatedAt = DateTime.UtcNow;
        _context.Complaints.Update(complaint);
        await _context.SaveChangesAsync();
        return complaint;
    }

    public async Task<bool> DeleteAsync(int complaintId)
    {
        var complaint = await _context.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return false;

        _context.Complaints.Remove(complaint);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int complaintId, int statusId, string changedBy, string? notes = null)
    {
        var complaint = await _context.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return false;

        var oldStatusId = complaint.StatusId;
        complaint.StatusId = statusId;
        complaint.UpdatedAt = DateTime.UtcNow;

        if (statusId == 4) // Resolved status
        {
            complaint.ResolvedAt = DateTime.UtcNow;
        }

        var history = new ComplaintHistory
        {
            ComplaintId = complaintId,
            OldStatusId = oldStatusId,
            NewStatusId = statusId,
            ChangedBy = changedBy,
            Notes = notes,
            ChangedAt = DateTime.UtcNow
        };

        _context.ComplaintHistories.Add(history);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ComplaintHistory>> GetHistoryAsync(int complaintId)
    {
        return await _context.ComplaintHistories
            .Where(h => h.ComplaintId == complaintId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Evidence>> GetEvidencesAsync(int complaintId)
    {
        return await _context.Evidences
            .Where(e => e.ComplaintId == complaintId)
            .OrderByDescending(e => e.UploadedAt)
            .ToListAsync();
    }
}
