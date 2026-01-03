using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    Role Role,
    bool IsBlocked,
    DateTime CreatedAt
);
