namespace Hospital.Application.DTOs;

public record CloseAppointmentRequest(
    string? DoctorNotes = null
);