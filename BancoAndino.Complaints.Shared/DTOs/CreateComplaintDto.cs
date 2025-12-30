namespace BancoAndino.Complaints.Shared.DTOs;

public record CreateComplaintDto(
    string CustomerId,
    string Title,
    string Description,
    int CategoryId,
    string Priority = "Medium"
);
