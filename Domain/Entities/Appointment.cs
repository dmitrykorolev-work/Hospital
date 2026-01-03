using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }

    public DateTime AppointmentTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public string? Notes { get; set; }
    public string? DoctorNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
