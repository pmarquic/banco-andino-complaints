using BancoAndino.Complaints.Shared.Models;
using BancoAndino.Complaints.Shared.DTOs;

namespace BancoAndino.Complaints.Shared.Interfaces;

public interface IComplaintRepository
{
    Task<Complaint?> GetByIdAsync(int complaintId);
    Task<IEnumerable<Complaint>> GetAllAsync(int? statusId = null, int? categoryId = null, string? customerId = null);
    Task<Complaint> CreateAsync(Complaint complaint);
    Task<Complaint> UpdateAsync(Complaint complaint);
    Task<bool> DeleteAsync(int complaintId);
    Task<bool> UpdateStatusAsync(int complaintId, int statusId, string changedBy, string? notes = null);
    Task<IEnumerable<ComplaintHistory>> GetHistoryAsync(int complaintId);
    Task<IEnumerable<Evidence>> GetEvidencesAsync(int complaintId);
}
