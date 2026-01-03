using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    DateTime AppointmentTime,
    AppointmentStatus Status,
    string? Notes,
    string? DoctorNotes,
    DateTime CreatedAt
);
