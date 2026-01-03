using Hospital.Application.DTOs;
using Hospital.Application.Mappings;
using Hospital.ConsoleClient.Interfaces;
using Spectre.Console;
using Newtonsoft.Json;

namespace Hospital.ConsoleClient.Menu;

internal class DoctorMenu : IMenu
{
    private readonly IRequestsService _requests;
    private readonly IPagedTable _pagedTable;
    private readonly AppMapper _mapper;

    public DoctorMenu(IRequestsService requests, IPagedTable pagedTable, AppMapper mapper)
    {
        _requests = requests;
        _pagedTable = pagedTable;
        _mapper = mapper;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Clear();

            string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[magenta]=== Doctor MENU ===[/]")
                .AddChoices(["Appointments", "Close appointment", "Exit"])
                .PageSize(15)
                );

            await (choice switch
            {
                "Appointments" => DoAppointmentsSubMenu(),
                "Close appointment" => DoCloseSubMenu(),
                "Exit" => Task.CompletedTask,
                _ => Task.CompletedTask
            });

            if (choice == "Exit")
                return;
        }
    }

    private async Task DoAppointmentsSubMenu()
    {
        int? page = 1;

        while (page.HasValue)
        {
            PagedResult<AppointmentDto>? data = null;

            try
            {
                data = await _requests.GetAppointmentsForDoctorAsync(new AppointmentQueryDto
                {
                    Page = page.Value,
                    PageSize = 10,
                    SortBy = "timestamp",
                    SortDir = "desc"
                });
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
                AnsiConsole.MarkupLine("[gray]Press <Enter> to continue[/]");
                Console.ReadLine();
                return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to retrieve appointments: {Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine("[gray]Press <Enter> to continue[/]");
                Console.ReadLine();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine("[red]Server returned empty response while retrieving appointments.[/]");
                AnsiConsole.MarkupLine("[gray]Press <Enter> to continue[/]");
                Console.ReadLine();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private async Task DoCloseSubMenu()
    {
        // Select appointment time
        var appointmentId = AnsiConsole.Prompt(
            new TextPrompt<Guid>("[yellow]Appointment ID: [/]")
        );

        // Optional notes
        var notes = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Notes (optional): [/]")
                .AllowEmpty()
        ).Trim();

        // Confirm
        var summary = $"[yellow]Close appointment with ID:[/] [cyan]{appointmentId}[/]" +
                      (string.IsNullOrWhiteSpace(notes) ? "" : $"[yellow] And with notes:[/] [cyan]\"{Markup.Escape(notes)}\"[/]") + "?";

        bool confirm = AnsiConsole.Confirm(summary);

        if (!confirm)
            return;

        try
        {
            await _requests.CloseAppointmentAsync(appointmentId, new CloseAppointmentRequest(notes));
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
            AnsiConsole.MarkupLine(" [gray]Press <Enter> to continue[/]");
            Console.ReadLine();
            return;
        }
        catch (ArgumentException ex)
        {
            string message = ex.Message;

            try
            {
                message = JsonConvert.DeserializeObject<AppointmentBookResultDto>(message).Message;
            }
            catch { /* Ignore */ }

            AnsiConsole.MarkupLine($"[red]Invalid request: {Markup.Escape(message)}[/]");
            AnsiConsole.MarkupLine("[gray]Press <Enter> to continue[/]");
            Console.ReadLine();
            return;
        }
        catch (Exception ex)
        {
            string message = ex.Message;

            try
            {
                message = JsonConvert.DeserializeObject<AppointmentBookResultDto>(message).Message;
            }
            catch { /* Ignore */ }

            AnsiConsole.MarkupLine($"[red]Failed to close appointment: {Markup.Escape(message)}[/]");
            AnsiConsole.MarkupLine("[gray]Press <Enter> to continue[/]");
            Console.ReadLine();
            return;
        }

        AnsiConsole.MarkupLine($"[green]Appointment closed successfully.[/]");

        AnsiConsole.MarkupLine("[gray]Press <Enter> to continue[/]");
        Console.ReadLine();
    }
}