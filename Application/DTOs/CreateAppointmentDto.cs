namespace Hospital.Application.DTOs;

public record CreateAppointmentDto(
    Guid PatientId,
    Guid DoctorId,
    DateTime AppointmentTime,
    string? Notes = null
    //string? DoctorNotes = null
);
