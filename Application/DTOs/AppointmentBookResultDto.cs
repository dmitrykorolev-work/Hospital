namespace Hospital.Application.DTOs;

public record AppointmentBookResultDto(
    bool Success,
    string? Message,
    Guid? AppointmentId,
    string? DoctorFirstName,
    string? DoctorLastName
);