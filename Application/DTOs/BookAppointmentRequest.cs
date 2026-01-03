using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record BookAppointmentRequest(
    DateTime AppointmentTime,
    Specialty Specialty,
    string? Notes = null
);