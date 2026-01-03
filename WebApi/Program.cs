using Hospital.WebApi.Auth;
using Hospital.WebApi.Middlewares;
using Microsoft.AspNetCore.Authentication;

namespace Hospital.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        // Adding authentication: setting default schemes and registering handler
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Token";
            options.DefaultChallengeScheme = "Token";
        })
        .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>( "Token", options => { });

        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // TODO: Set up certificates and enable https
        //app.UseHttpsRedirection();

        app.MapControllers();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.Run();
    }
}