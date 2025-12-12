namespace BancoAndino.Complaints.Shared.Models;

public class ComplaintStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
