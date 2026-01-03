using Hospital.Infrastructure;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace Hospital.WebApi;

public static class WebApiServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext - connection string from configuration
        services.AddDbContextFactory<HospitalDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString( "Default" ) ?? "Data Source=hospital.db" ));

        services.AddSwaggerGen(options => // Enable authentication in Swagger
        {
            options.AddSecurityDefinition( "bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "Token",
                Description = "Authorization header using the Bearer scheme."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference( "bearer", document)] = []
            });
        });

        // Add infrastructure services from the Infrastructure project

        DependencyInjection.AddInfrastructure(services, configuration);

        return services;
    }
}