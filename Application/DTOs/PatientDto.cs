namespace Hospital.Application.DTOs;

public record PatientDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime BirthDate,
    string Phone,
    string Email,

    DateTime CreatedAt
);
