namespace Hospital.Application.DTOs;

public record ReportRequestDto(
    DateTime? From = null,
    DateTime? To = null,
    Guid? DoctorId = null,
    Guid? PatientId = null
);
