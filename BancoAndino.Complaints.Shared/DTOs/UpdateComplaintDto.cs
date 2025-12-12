namespace BancoAndino.Complaints.Shared.DTOs;

public record UpdateComplaintDto(
    string? Title,
    string? Description,
    int? CategoryId,
    string? Priority,
    string? AssignedTo
);
