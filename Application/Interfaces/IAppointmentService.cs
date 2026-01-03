using Hospital.Application.DTOs;
using Hospital.Domain.Enums;

namespace Hospital.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto);
    Task UpdateAsync(AppointmentDto dto);

    Task<AppointmentDto?> GetByIdAsync(Guid id);
    Task<PagedResult<AppointmentDto>> SearchAsync(AppointmentQueryDto query);

    Task<bool> DeleteAsync(Guid id);

    Task<DoctorDto?> FindAvailableDoctorAsync(DateTime appointmentTime, Specialty specialty);

    Task CloseAsync(Guid appointmentId, Guid doctorId, string? doctorNotes);
}