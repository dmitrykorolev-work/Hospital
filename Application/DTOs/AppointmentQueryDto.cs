using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record AppointmentQueryDto(
    int Page = 1,
    int PageSize = 20,
    Guid? DoctorId = null,
    Guid? PatientId = null,
    DateTime? From = null,
    DateTime? To = null,
    AppointmentStatus? Status = null,
    string? SortBy = "AppointmentTime",
    string? SortDir = "asc"
);
