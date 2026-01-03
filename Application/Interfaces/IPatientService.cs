using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface IPatientService
{
    Task<PatientDto> CreateAsync(CreatePatientDto dto);
    Task UpdateAsync(PatientDto dto);
    Task<PatientDto?> GetByIdAsync(Guid id);
    Task<PatientDto?> GetByUserIdAsync(Guid userId);
    Task<PagedResult<PatientDto>> SearchAsync(PatientQueryDto query);
    Task DeleteAsync(Guid id);
}