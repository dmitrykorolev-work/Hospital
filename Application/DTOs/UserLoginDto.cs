namespace Hospital.Application.DTOs;

public record UserLoginDto(
    string Email,
    string Password
);