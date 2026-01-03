using Hospital.Application.DTOs;
using Hospital.Domain.Entities;

namespace Hospital.Application.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Appointment>> GetAllAsync();
    Task<IEnumerable<Appointment>> SearchAsync(AppointmentQueryDto query);
    Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<Appointment>> GetByDoctorIdAsync(Guid doctorId);

    Task AddAsync(Appointment appointment);
    Task UpdateAsync(Appointment appointment);
    Task<bool> DeleteAsync(Guid id);
}
