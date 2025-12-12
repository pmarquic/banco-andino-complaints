namespace BancoAndino.Complaints.Shared.Models;

public class ComplaintHistory
{
    public int HistoryId { get; set; }
    public int ComplaintId { get; set; }
    public int? OldStatusId { get; set; }
    public int NewStatusId { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Complaint? Complaint { get; set; }
}
