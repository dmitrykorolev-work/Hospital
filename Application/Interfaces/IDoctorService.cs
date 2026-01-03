using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface IDoctorService
{
    Task<DoctorDto> CreateAsync(CreateDoctorDto dto);
    Task UpdateAsync(DoctorDto dto);

    Task<DoctorDto?> GetByIdAsync(Guid id);
    Task<DoctorDto?> GetByUserIdAsync(Guid userId);
    Task<PagedResult<DoctorDto>> SearchAsync(DoctorQueryDto query);
    Task DeleteAsync(Guid id);
}
