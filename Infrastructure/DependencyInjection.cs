using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;
using Hospital.Application.Services;
using Hospital.Application.Settings;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services required by the application.
    /// Called from WebApiServiceRegistration to add shared infrastructure services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Options for AppointmentService
        services.Configure<AppointmentOptions>(configuration.GetSection( "AppointmentOptions" ));

        // Repositories
        services.AddScoped<IUserRepository, SqliteUserRepository>();
        services.AddScoped<IPatientRepository, SqlitePatientRepository>();
        services.AddScoped<IDoctorRepository, SqliteDoctorRepository>();
        services.AddScoped<IAppointmentRepository, SqliteAppointmentRepository>();
        services.AddScoped<IAuditRepository, SqliteAuditRepository>();

        // Domain Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IReportService, ReportService>();

        // Mapper
        services.AddSingleton<AppMapper>();

        // PasswordHasher
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // Session Service (In-Memory for simplicity. Replace with distributed cache in prod)
        services.AddSingleton<ISessionService, InMemorySessionService>();

        return services;
    }
}