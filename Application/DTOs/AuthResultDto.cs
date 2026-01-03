using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record AuthResultDto(
    bool Success,
    Guid? Token,
    Role? Role,
    string? Message
);