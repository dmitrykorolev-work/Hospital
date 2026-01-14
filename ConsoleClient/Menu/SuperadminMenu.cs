using Hospital.Application.DTOs;
using Hospital.Application.Mappings;
using Hospital.ConsoleClient.Interfaces;
using Hospital.Domain.Enums;
using Spectre.Console;

namespace Hospital.ConsoleClient.Menu;

internal class SuperadminMenu : AdminMenu
{
    public SuperadminMenu(IRequestsService requests, IPagedTable pagedTable, IRegisterHelper registerHelper, AppMapper mapper)
        : base(requests, pagedTable, registerHelper, mapper) { }

    public async override Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Clear();

            string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[magenta]=== SUPER ADMIN MENU ===[/]")
                .AddChoices(["Patients", "Doctors", "Users", "Audit", "Report", "Ban user", "Unban user", "Doctor registration", "Admin registration", "Export CSV", "Import CSV", "Exit"])
                .PageSize(15)
                );

            await(choice switch
            {
                "Patients" => DoPatientsSubMenu(),
                "Doctors" => DoDoctorsSubMenu(),
                "Users" => DoUsersSubMenu(),
                "Audit" => DoAuditSubMenu(),
                "Report" => DoReportSubMenu(),
                "Ban user" => DoBanSubMenu(true),
                "Unban user" => DoUnbanSubMenu(true),
                "Doctor registration" => DoDoctorRegistrationSubMenu(),
                "Admin registration" => DoAdminRegistrationSubMenu(),
                "Export CSV" => DoExportSubMenu(),
                "Import CSV" => DoImportSubMenu(),
                "Exit" => Task.CompletedTask,
                _ => Task.CompletedTask
            });

            if (choice == "Exit")
                return;
        }
    }

    private protected async Task DoImportSubMenu()
    {
        var importOptions = new[]
        {
            "Patients",
            "Doctors",
            "Users",
            "Appointments",
            "Exit"
        };

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[magenta]=== IMPORT CSV MENU ===[/]")
                .AddChoices(importOptions)
        );

        if (choice == "Exit")
            return;

        // Ask for file path
        string filePath = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]File path: [/]")
                .Validate(input => !string.IsNullOrWhiteSpace(input) && File.Exists(input),
                    "[red]Please enter a valid file path[/]"));

        byte[] content;

        try
        {
            content = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to read file: {Markup.Escape(ex.Message)}[/]");
            Pause();
            return;
        }

        ImportResultDto? result = null;

        try
        {
            result = choice switch
            {
                "Patients" => await _requests.ImportPatientsAsync(content),
                "Doctors" => await _requests.ImportDoctorsAsync(content),
                "Users" => await _requests.ImportUsersAsync(content),
                "Appointments" => await _requests.ImportAppointmentsAsync(content),
                _ => null
            };
        }
        catch (KeyNotFoundException)
        {
            AnsiConsole.MarkupLine("[red]Import failed: target resource not found (404).[/]");
            Pause();
            return;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
            Pause();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Import failed: {Markup.Escape(ex.Message)}[/]");
            Pause();
            return;
        }

        if (result is null)
        {
            AnsiConsole.MarkupLine("[red]No result returned from import.[/]");
            Pause();
            return;
        }

        AnsiConsole.MarkupLine("[green]Import summary:[/]");
        AnsiConsole.MarkupLine($"  [yellow]Processed:[/] {result.Processed}");
        AnsiConsole.MarkupLine($"  [yellow]Updated:[/] {result.Updated}");
        AnsiConsole.MarkupLine($"  [yellow]Skipped:[/] {result.Skipped}");

        if (result.Errors != null && result.Errors.Any())
        {
            AnsiConsole.MarkupLine("[red]Errors:[/]");
            foreach (var err in result.Errors)
            {
                AnsiConsole.MarkupLine($"  - {Markup.Escape(err)}");
            }
        }

        Pause();
    }

    private protected async Task DoAdminRegistrationSubMenu()
    {
        var email = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Email: [/]")
                .Validate(input => input.Contains("@") && input.Contains("."),
                    "[red]Please enter a valid email address[/]")
        );

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Password: [/]")
                .Secret()
                .Validate(value => value.Length > 8, "[red]Password must be at least 8 characters long![/]")
        );

        var passwordn = AnsiConsole.Prompt(
            new TextPrompt<string>("[yellow]Retype password: [/]")
                .Secret()
        );

        if (password != passwordn)
        {
            AnsiConsole.MarkupLine("[red]Passwords do not match[/]");
            Pause();
            return;
        }

        try
        {
            AdminRegisterDto adminRegisterDto = new AdminRegisterDto
            (
                email,
                password
            );

            await _requests.CreateAdminAsync(adminRegisterDto);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to register admin: { Markup.Escape(ex.Message) }[/]");
            Pause();
            return;
        }

        AnsiConsole.MarkupLine("[green]Admin registered successfully![/]");
        Pause();
    }
}