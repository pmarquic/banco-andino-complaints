namespace BancoAndino.Complaints.Shared.DTOs;

public record UpdateStatusDto(
    int StatusId,
    string ChangedBy,
    string? Notes = null
);
