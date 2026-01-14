using Hospital.Application.Interfaces;
using Hospital.Application.DTOs;
using Hospital.Domain.Enums;

namespace Hospital.WebApi.Initializers;

public class SuperadminInitializer : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SuperadminInitializer> _logger;

    public SuperadminInitializer(IServiceProvider services, ILogger<SuperadminInitializer> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            string? password = Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");

            if (password is null || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Superadmin password not provided. Set env SUPERADMIN_PASSWORD");
                password = "superadmin1234"; // Default password (if not provided)
            }

            const string email = "superadmin";

            var existing = await userService.GetByEmailAsync(email).ConfigureAwait(false);

            if (existing is null)
            {
                var createDto = new CreateUserDto(email, password, Role.Superadmin);
                await userService.CreateAsync(createDto).ConfigureAwait(false);
                _logger.LogInformation("Superadmin user created (email: {Email}).", email);
            }
            else
            {
                // Update password and role if needed
                await userService.ChangePasswordAsync(existing.Id, password).ConfigureAwait(false);

                if (existing.Role != Role.Superadmin)
                {
                    await userService.ChangeRoleAsync(existing.Id, Role.Superadmin).ConfigureAwait(false);
                }

                // Unblock user if blocked
                await userService.UnblockAsync(existing.Id).ConfigureAwait(false);

                _logger.LogInformation($"Superadmin user updated (Email: {email})");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure superadmin user.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}