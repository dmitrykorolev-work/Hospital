using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs;

public record UserRegisterDto(
    [param: Required, EmailAddress] string Email,
    [param: Required, MinLength(8)] string Password,
    [param: Required, MinLength(1)] string FirstName,
    [param: Required, MinLength(1)] string LastName,
    [param: Required, Phone] string Phone,
    [param: Required] DateTime BirthDate
);