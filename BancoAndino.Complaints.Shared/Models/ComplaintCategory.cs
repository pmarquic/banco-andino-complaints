namespace BancoAndino.Complaints.Shared.Models;

public class ComplaintCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultPriority { get; set; } = "Medium";
    public int SlaHours { get; set; } = 48;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
