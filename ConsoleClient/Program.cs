using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Hospital.ConsoleClient.Helpers;
using Hospital.ConsoleClient.Interfaces;
using Hospital.ConsoleClient.Menu;
using Hospital.Application.Mappings;

namespace Hospital.ConsoleClient;

class Program
{
    static async Task Main(string[] args)
    {
        string ApiBaseURL = "http://localhost:5084"; // TODO: Move to config

        Console.OutputEncoding = System.Text.Encoding.UTF8;

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton(new HttpClient // So auth token will be preserved between requests
                {
                    BaseAddress = new Uri(ApiBaseURL)
                });

                services.AddTransient<IRequestsService, RequestsService>();

                services.AddTransient<IPagedTable, PagedTable>();
                services.AddTransient<IRegisterHelper, RegisterHelper>();
                services.AddTransient<AppMapper>();

                services.AddTransient<MainMenu>();
                services.AddTransient<AdminMenu>();
                services.AddTransient<PatientMenu>();
                services.AddTransient<DoctorMenu>();
            })
            .Build();

        var requestsService = host.Services.GetRequiredService<IRequestsService>();

        var menu = host.Services.GetRequiredService<MainMenu>();
        await menu.RunAsync();
    }
}