using Hospital.Application.DTOs;
using Hospital.ConsoleClient.Interfaces;
using Hospital.Domain.Enums;
using Spectre.Console;
using Newtonsoft.Json;

namespace Hospital.ConsoleClient.Menu;

internal class PatientMenu : IMenu
{
    private readonly IRequestsService _requests;
    private readonly IPagedTable _pagedTable;

    public PatientMenu(IRequestsService requests, IPagedTable pagedTable)
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
                .Title( "[magenta]=== Patient MENU ===[/]" )
                .AddChoices( ["Appointments", "Create appointment", "Exit"] )
                .PageSize(15)
                );

            await (choice switch
            {
                "Appointments" => DoAppointmentsSubMenu(),
                "Create appointment" => DoBookSubMenu(),
                "Exit" => Task.CompletedTask,
                _ => Task.CompletedTask
            });

            if (choice == "Exit" )
                return;
        }
    }

    private protected static void Pause()
    {
        AnsiConsole.MarkupLine("[gray]Press <Enter> to continue[/]");
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
                data = await _requests.GetAppointmentsForPatientAsync(new AppointmentQueryDto
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
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve appointments: { Markup.Escape(ex.Message) }[/]" );
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

    private async Task DoBookSubMenu()
    {
        // Select specialty
        var specialty = AnsiConsole.Prompt(
            new SelectionPrompt<Specialty>()
                .Title( "[yellow]Select doctor specialty:[/]" )
                .AddChoices(Enum.GetValues<Specialty>())
        );

        // Select appointment time
        var appointmentTime = AnsiConsole.Prompt(
            new TextPrompt<DateTime>( "[yellow]Appointment time (YYYY-MM-DD HH:mm): [/]" )
                .Validate(dt =>
                {
                    if (dt <= DateTime.Now)
                        return ValidationResult.Error( "[red]Appointment time must be in the future.[/]" );
                    return ValidationResult.Success();
                })
        );

        // Optional notes
        var notes = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Notes (optional): [/]" )
                .AllowEmpty()
        ).Trim();

        // Confirm
        var summary = $"Specialty: [cyan]{specialty}[/]\nTime: [cyan]{appointmentTime:yyyy-MM-dd HH:mm}[/]" +
                      (string.IsNullOrWhiteSpace(notes) ? "" : $"\nNotes: [cyan]{Markup.Escape(notes)}[/]" );

        bool confirm = AnsiConsole.Confirm( $"Book appointment:\n{summary}\n" );

        if (!confirm)
            return;

        AppointmentBookResultDto? result = null;

        try
        {
            result = await _requests.BookAppointmentAsync(new BookAppointmentRequest(
                appointmentTime,
                specialty,
                string.IsNullOrWhiteSpace(notes) ? null : notes
            ));
        }
        catch (InvalidOperationException)
        {
            AnsiConsole.MarkupLine("[red]Booking failed: No available doctor for requested time and specialty.[/]");
            Pause();
            return;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine( "[red]Operation cancelled.[/]" );
            Pause();
            return;
        }
        catch (ArgumentException ex)
        {
            string message = ex.Message;

            try {
                message = JsonConvert.DeserializeObject<AppointmentBookResultDto>(message).Message ?? message;
            } catch { /* Ignore */ }

            AnsiConsole.MarkupLine( $"[red]Invalid request: { Markup.Escape(message) }[/]" );
            Pause();
            return;
        }
        catch (Exception ex)
        {
            string message = ex.Message;

            try {
                message = JsonConvert.DeserializeObject<AppointmentBookResultDto>(message).Message ?? message;
            }
            catch { /* Ignore */ }

            AnsiConsole.MarkupLine( $"[red]Failed to book appointment: { Markup.Escape(message) }[/]" );
            Pause();
            return;
        }

        if (result is null)
        {
            AnsiConsole.MarkupLine( "[red]Server returned empty response while booking appointment.[/]");
            Pause();
            return;
        }

        if (!result.Success)
        {
            AnsiConsole.MarkupLine( $"[red]Booking failed: { Markup.Escape(result.Message ?? "Unknown error" ) }[/]" );
            Pause();
            return;
        }

        // Success
        var doctorName = string.IsNullOrWhiteSpace(result.DoctorFirstName) && string.IsNullOrWhiteSpace(result.DoctorLastName)
            ? "[NONAME]"
            : $"{result.DoctorFirstName} {result.DoctorLastName}".Trim();

        AnsiConsole.MarkupLine( $"[green]Appointment booked successfully.[/]" );
        AnsiConsole.MarkupLine( $"[yellow]Doctor:[/] [cyan]{Markup.Escape(doctorName)}[/]" );

        if (result.AppointmentId.HasValue)
        {
            AnsiConsole.MarkupLine( $"[yellow]Appointment Id:[/] [cyan]{result.AppointmentId}[/]" );
        }

        Pause();
    }
}