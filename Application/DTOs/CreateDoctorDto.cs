using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record CreateDoctorDto(
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime BirthDate,
    string Email,
    string Phone,

    Specialty Specialty
);
