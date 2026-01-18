using Hospital.Application.DTOs;
using Hospital.ConsoleClient.Interfaces;
using Spectre.Console;
using Newtonsoft.Json;

namespace Hospital.ConsoleClient.Menu;

internal class DoctorMenu : IMenu
{
    private readonly IRequestsService _requests;
    private readonly IPagedTable _pagedTable;

    public DoctorMenu(IRequestsService requests, IPagedTable pagedTable)
    {
        _requests = requests;
        _pagedTable = pagedTable;
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
    private protected static void Pause()
    {
        AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
        Console.ReadLine();
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
                AnsiConsole.MarkupLine( "[red]Operation cancelled.[/]" );
                Pause();
                return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve appointments: {Markup.Escape(ex.Message)}[/]" );
                Pause();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving appointments.[/]" );
                Pause();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private async Task DoCloseSubMenu()
    {
        var appointmentId = AnsiConsole.Prompt(
            new TextPrompt<Guid>( "[yellow]Appointment ID: [/]" )
        );

        // Optional notes
        var notes = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Notes (optional): [/]" )
                .AllowEmpty()
        ).Trim();

        // Confirm
        var summary = $"[yellow]Close appointment with ID:[/] [cyan]{ appointmentId }[/]" +
                      (string.IsNullOrWhiteSpace(notes) ? "" : $"[yellow] And with notes:[/] [cyan]\"{Markup.Escape(notes)}\"[/]") + "?";

        bool confirm = AnsiConsole.Confirm(summary);

        if (!confirm)
            return;

        try
        {
            await _requests.CloseAppointmentAsync(appointmentId, new CloseAppointmentRequest(notes));
            AnsiConsole.MarkupLine($"[green]Appointment closed successfully.[/]");
        }
        catch (KeyNotFoundException)
        {
            AnsiConsole.MarkupLine("[red]Appointment does not exists![/]");
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine( "[red]Operation cancelled.[/]" );
        }
        catch (ArgumentException ex)
        {
            string message = ex.Message;

            try
            {
                message = JsonConvert.DeserializeObject<AppointmentBookResultDto>(message).Message ?? message;
            }
            catch { /* Ignore */ }

            AnsiConsole.MarkupLine( $"[red]Invalid request: { Markup.Escape(message) }[/]" );
        }
        catch (Exception ex)
        {
            string message = ex.Message;

            try
            {
                message = JsonConvert.DeserializeObject<AppointmentBookResultDto>(message).Message ?? message;
            }
            catch { /* Ignore */ }

            AnsiConsole.MarkupLine( $"[red]Failed to close appointment: { Markup.Escape(message) }[/]" );
        }

        Pause();
    }
}