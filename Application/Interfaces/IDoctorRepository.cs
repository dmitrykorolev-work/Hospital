using Hospital.Application.DTOs;
using Hospital.Domain.Entities;

namespace Hospital.Application.Interfaces;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(Guid id);
    Task<IEnumerable<Doctor>> GetAllAsync();
    Task<IEnumerable<Doctor>> SearchAsync(DoctorQueryDto query);
    Task AddAsync(Doctor doctor);
    Task UpdateAsync(Doctor doctor);
    Task<bool> DeleteAsync(Guid id);
    Task<Doctor?> GetByUserIdAsync(Guid userId);
}