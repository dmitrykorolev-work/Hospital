using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record CreateUserDto(
    string Email,
    string Password,
    Role Role
);
