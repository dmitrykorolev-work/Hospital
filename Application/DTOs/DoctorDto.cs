using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record DoctorDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime BirthDate,
    string Phone,
    string Email,
    Specialty Specialty,

    DateTime CreatedAt
);
