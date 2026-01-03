using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Hospital.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ISessionService _sessionService;
    private readonly AppMapper _mapper;

    public AuthService(
        IUserService userService,
        IUserRepository userRepository,
        IPatientService patientService,
        IDoctorService doctorService,
        IPasswordHasher<User> passwordHasher,
        ISessionService sessionService,
        AppMapper mapper)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AuthResultDto> RegisterAsync(UserRegisterDto dto, Role role, Specialty? specialty = null)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Email)) return new AuthResultDto(false, null, null, "Email is required." );
        if (string.IsNullOrWhiteSpace(dto.Password)) return new AuthResultDto(false, null, null, "Password is required." );

        if (await _userRepository.GetByEmailAsync(dto.Email).ConfigureAwait(false) is not null)
        {
            return new AuthResultDto( false, null, null, "Email is already registered." );
        }

        if (dto.BirthDate >= DateTime.UtcNow)
        {
            return new AuthResultDto( false, null, null, "Birth date must be in the past." ); // Come back when you're born! TODO: Age limit?
        }

        try
        {
            // Create a user (UserService hashes the password)
            var createUserDto = new CreateUserDto(dto.Email, dto.Password, role);
            var createdUser = await _userService.CreateAsync(createUserDto).ConfigureAwait(false);

            // Create a related entity depending on the role
            try
            {
                if (role == Role.Patient)
                {
                    var patientDto = new CreatePatientDto(
                        createdUser.Id,
                        dto.FirstName,
                        dto.LastName,
                        dto.BirthDate,
                        dto.Email,
                        dto.Phone);

                    await _patientService.CreateAsync(patientDto).ConfigureAwait(false);
                }
                else if (role == Role.Doctor)
                {
                    var doctorDto = new CreateDoctorDto(
                        createdUser.Id,
                        dto.FirstName,
                        dto.LastName,
                        dto.BirthDate,
                        dto.Email,
                        dto.Phone,
                        specialty ?? Specialty.GeneralPractitioner);

                    await _doctorService.CreateAsync(doctorDto).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Failed to create patient/doctor, returning an error.
                // TODO: Roll back the user in the future.

                return new AuthResultDto(false, null, null, $"Failed to create linked entity: {ex.Message}" );
            }

            // The default TTL is 24 hours.
            // TODO: Move TTL to appsettings.
            var tokenGuid = await _sessionService.CreateSessionAsync(createdUser.Id, TimeSpan.FromHours(24)).ConfigureAwait(false);

            return new AuthResultDto(true, tokenGuid, createdUser.Role, null);
        }
        catch (InvalidOperationException ex)
        {
            return new AuthResultDto(false, null, null, ex.Message);
        }
        catch (Exception ex)
        {
            return new AuthResultDto(false, null, null, $"Registration failed: {ex.Message}" );
        }
    }

    public async Task<AuthResultDto> LoginAsync(UserLoginDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return new AuthResultDto(false, null, null, "Email and password are required." );
        }

        var user = await _userRepository.GetByEmailAsync(dto.Email).ConfigureAwait(false);

        if (user is null) return new AuthResultDto(false, null, null, "Invalid credentials." );
        if (user.IsBlocked) return new AuthResultDto(false, null, null, "User is blocked." );

        var verification = await _userService.VerifyHashedPassword(user.Id, dto.Password);
        if (verification == PasswordVerificationResult.Failed) return new AuthResultDto(false, null, null, "Invalid credentials." );

        // The default TTL is 24 hours.
        // TODO: Move TTL to appsettings.
        var tokenGuid = await _sessionService.CreateSessionAsync(user.Id, TimeSpan.FromHours(24)).ConfigureAwait(false);

        return new AuthResultDto(true, tokenGuid, user.Role, null);
    }

    public async Task LogoutAsync(Guid token)
    {
        await _sessionService.RevokeSessionAsync(token).ConfigureAwait(false);
    }
}