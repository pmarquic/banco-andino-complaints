namespace BancoAndino.Complaints.Shared.DTOs;

public record ComplaintResponseDto(
    int ComplaintId,
    string CustomerId,
    string Title,
    string Description,
    int StatusId,
    string StatusName,
    int CategoryId,
    string CategoryName,
    string Priority,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SlaDeadline,
    DateTime? ResolvedAt,
    string? ResolutionNotes,
    IEnumerable<EvidenceDto> Evidences
);

public record EvidenceDto(
    int EvidenceId,
    string BlobUrl,
    string FileName,
    long FileSize,
    string ContentType,
    DateTime UploadedAt,
    string UploadedBy
);
