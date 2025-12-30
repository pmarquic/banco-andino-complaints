namespace BancoAndino.Complaints.Shared.Models;

public class Complaint
{
    public int ComplaintId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public int CategoryId { get; set; }
    public string Priority { get; set; } = "Medium";
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SlaDeadline { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }

    // Navigation properties
    public ComplaintStatus? Status { get; set; }
    public ComplaintCategory? Category { get; set; }
    public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
    public ICollection<ComplaintHistory> History { get; set; } = new List<ComplaintHistory>();
}
