namespace BancoAndino.Complaints.Shared.Models;

public class Evidence
{
    public int EvidenceId { get; set; }
    public int ComplaintId { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;

    // Navigation property
    public Complaint? Complaint { get; set; }
}
