using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Hospital.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly AppMapper _mapper;

    public UserService(IUserRepository userRepository, AppMapper mapper, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentException( "Email is required.", nameof(dto.Email));
        if (string.IsNullOrWhiteSpace(dto.Password)) throw new ArgumentException( "Password is required.", nameof(dto.Password));

        var existing = await _userRepository.GetByEmailAsync(dto.Email).ConfigureAwait(false);
        if (existing is not null) throw new InvalidOperationException( "User with given email already exists." );

        var user = _mapper.CreateUserDtoToUser(dto);

        // Ensure unique Id (I'm out of ideas for jokes)
        do user.Id = Guid.NewGuid();
        while (await _userRepository.GetByIdAsync(user.Id).ConfigureAwait(false) is not null);

        user.CreatedAt = DateTime.UtcNow;
        user.IsBlocked = false;
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user).ConfigureAwait(false);

        return _mapper.UserToUserDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
        return user is null ? null : _mapper.UserToUserDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email).ConfigureAwait(false);
        return user is null ? null : _mapper.UserToUserDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync().ConfigureAwait(false);
        return _mapper.UsersToUserDtos(users);
    }

    public async Task ChangeRoleAsync(Guid userId, Role role)
    {
        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
        if (user is null) throw new KeyNotFoundException( "User not found." );
        user.Role = role;
        await _userRepository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task BlockAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
        if (user is null) throw new KeyNotFoundException( "User not found." );
        user.IsBlocked = true;
        await _userRepository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task UnblockAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
        if (user is null) throw new KeyNotFoundException( "User not found." );
        user.IsBlocked = false;
        await _userRepository.UpdateAsync(user).ConfigureAwait(false);
    }
    public async Task<bool> IsBlocked(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
        if (user is null) throw new KeyNotFoundException( "User not found." );
        return user.IsBlocked;
    }

    public async Task<PasswordVerificationResult> VerifyHashedPassword(Guid userId, string password)
    {
        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
        if (user is null) throw new KeyNotFoundException( "User not found." );
        return _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
    }

    public async Task UpdateAsync(UserDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.Id == Guid.Empty) throw new ArgumentException( "Id is required.", nameof(dto.Id));
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentException( "Email is required.", nameof(dto.Email));

        var user = await _userRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (user is null) throw new KeyNotFoundException( "User not found." );

        // Check that another user is not using the same Email
        var byEmail = await _userRepository.GetByEmailAsync(dto.Email).ConfigureAwait(false);
        if (byEmail is not null && byEmail.Id != user.Id)
        {
            throw new InvalidOperationException( "Another user with the same email already exists." );
        }

        user.Email = dto.Email;
        user.Role = dto.Role;
        user.IsBlocked = dto.IsBlocked;
        // CreatedAt Id, and PasswordHash are not changed

        await _userRepository.UpdateAsync(user).ConfigureAwait(false);
    }
}