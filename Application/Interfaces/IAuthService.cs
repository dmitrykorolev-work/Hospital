using Hospital.Application.DTOs;
using Hospital.Domain.Enums;

namespace Hospital.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(UserRegisterDto dto, Role role, Specialty? specialty = null);

    Task<AuthResultDto> LoginAsync(UserLoginDto dto);

    Task LogoutAsync(Guid token);
}
