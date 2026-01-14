using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs;

public record AdminRegisterDto(
    [param: Required, EmailAddress] string Email,
    [param: Required, MinLength(8)] string Password
);