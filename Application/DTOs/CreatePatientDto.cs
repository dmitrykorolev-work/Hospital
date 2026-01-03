namespace Hospital.Application.DTOs;

public record CreatePatientDto(
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime BirthDate,
    string Email,
    string Phone
);