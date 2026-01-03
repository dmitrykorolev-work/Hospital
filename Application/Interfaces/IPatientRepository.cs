using Hospital.Application.DTOs;
using Hospital.Domain.Entities;

namespace Hospital.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id);
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<IEnumerable<Patient>> SearchAsync(PatientQueryDto query);
    Task AddAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task<bool> DeleteAsync(Guid id);
    Task<Patient?> GetByUserIdAsync(Guid userId);
}
