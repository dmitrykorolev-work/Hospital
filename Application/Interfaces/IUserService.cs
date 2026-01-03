using Hospital.Application.DTOs;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Hospital.Application.Interfaces;

public interface IUserService
{

    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task UpdateAsync(UserDto dto);

    Task<UserDto?> GetByIdAsync(Guid userId);

    Task<UserDto?> GetByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetAllAsync();

    Task ChangeRoleAsync(Guid userId, Role role);

    Task BlockAsync(Guid userId);
    Task UnblockAsync(Guid userId);

    Task<bool> IsBlocked(Guid userId);

    Task<PasswordVerificationResult> VerifyHashedPassword(Guid userId, string password);
}
