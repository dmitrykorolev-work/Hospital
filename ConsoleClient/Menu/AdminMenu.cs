using Hospital.Application.DTOs;
using Hospital.Application.Mappings;
using Hospital.ConsoleClient.Interfaces;
using Hospital.Domain.Enums;
using Spectre.Console;
using System.Reflection;

namespace Hospital.ConsoleClient.Menu;

internal class AdminMenu : IMenu // TODO: Separate into multiple files? (500+ lines of code omg)
{
    private protected readonly IRequestsService _requests;
    private protected readonly IPagedTable _pagedTable;
    private protected readonly IRegisterHelper _registerHelper;
    private protected readonly AppMapper _mapper;

    public AdminMenu(IRequestsService requests, IPagedTable pagedTable, IRegisterHelper registerHelper, AppMapper mapper)
    {
        _requests = requests;
        _pagedTable = pagedTable;
        _registerHelper = registerHelper;
        _mapper = mapper;
    }

    public async virtual Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Clear();

            string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title( "[magenta]=== ADMIN MENU ===[/]" )
                .AddChoices( ["Patients", "Doctors", "Users", "Audit", "Report", "Ban user", "Unban user", "Doctor registration", "Export CSV", "Exit"] )
                .PageSize(15)
                );

            await (choice switch
            {
                "Patients" => DoPatientsSubMenu(),
                "Doctors" => DoDoctorsSubMenu(),
                "Users" => DoUsersSubMenu(),
                "Audit" => DoAuditSubMenu(),
                "Report" => DoReportSubMenu(),
                "Ban user" => DoBanSubMenu(false),
                "Unban user" => DoUnbanSubMenu(false),
                "Doctor registration" => DoDoctorRegistrationSubMenu(),
                "Export CSV" => DoExportSubMenu(),
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

    private protected async Task DoPatientsSubMenu()
    {
        int? page = 1;

        while (page.HasValue)
        {
            PagedResult<PatientDto>? data = null;

            try
            {
                data = await _requests.SearchPatientsAsync(new PatientQueryDto
                {
                    Page = page.Value,
                    PageSize = 10
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
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve patients: { Markup.Escape(ex.Message) }[/]" );
                Pause();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving patients.[/]" );
                Pause();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private protected async Task DoDoctorsSubMenu()
    {
        int? page = 1;

        while (page.HasValue)
        {
            PagedResult<DoctorDto>? data = null;

            try
            {
                data = await _requests.SearchDoctorsAsync(new DoctorQueryDto
                {
                    Page = page.Value,
                    PageSize = 10
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
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve doctors: { Markup.Escape(ex.Message) }[/]" );
                Pause();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving doctors.[/]" );
                Pause();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private protected async Task DoUsersSubMenu()
    {
        int? page = 1;

        while (page.HasValue)
        {
            PagedResult<UserDto>? data = null;
            try
            {
                data = await _requests.GetUsersAsync(new UserQueryDto
                {
                    Page = page.Value,
                    PageSize = 10
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
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve users: { Markup.Escape(ex.Message) }[/]" );
                Pause();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving users.[/]" );
                Pause();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private protected async Task DoAuditSubMenu()
    {
        int? page = 1;

        while (page.HasValue)
        {
            PagedResult<AuditLogDto>? data = null;
            try
            {
                data = await _requests.SearchAuditAsync(new AuditLogQueryDto
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
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve audit logs: { Markup.Escape(ex.Message) }[/]" );
                Pause();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving audit logs.[/]" );
                Pause();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private protected async Task DoReportSubMenu()
    {
        ReportResultDto? result = null;

        try
        {
            result = await _requests.GenerateReportAsync( new ReportRequestDto() );
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine( "[red]Operation cancelled.[/]" );
            Pause();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Failed to generate report: { Markup.Escape(ex.Message) }[/]" );
            Pause();
            return;
        }

        if (result is null)
        {
            AnsiConsole.MarkupLine( "[red]Server returned empty report.[/]" );
            Pause();
            return;
        }

        var columns = typeof(ReportResultDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => p.Name)
            .ToArray();

        var rows = new[]
        {
            columns.Select(col =>
            {
                var prop = typeof(ReportResultDto).GetProperty(col);
                return prop?.GetValue(result)?.ToString() ?? string.Empty;
            }).ToArray()
        };

        var table = new Table().Centered().RoundedBorder();
        table.ShowRowSeparators = true;

        // Add columns
        table.AddColumns(columns);

        // Add rows
        foreach (var row in rows)
            table.AddRow(row);

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine( $"[gray]Q: quit[/]" );

        // Reading key input
        while (true)
        {
            var keyInfo = AnsiConsole.Console.Input.ReadKey(true);

            if (!keyInfo.HasValue) return;
            
            if (keyInfo.Value.Key == ConsoleKey.Q || keyInfo.Value.Key == ConsoleKey.Escape)
            {
                return;
            }
        }
    }

    private protected async Task DoBanSubMenu(bool superadmin)
    {
        var email = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Enter user email to ban: [/]" )
                .Validate(input => !string.IsNullOrWhiteSpace(input) && input.Contains( "@" ) && input.Contains( "." ),
                    "[red]Please enter a valid email address[/]" ))
            .Trim();

        UserDto? user = null;
        try
        {
            user = await _requests.GetUserByEmailAsync(email);
        }
        catch (KeyNotFoundException)
        {
            AnsiConsole.MarkupLine( "[red]User with specified email was not found.[/]" );
            Pause();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Error while searching user: { Markup.Escape(ex.Message) }[/]" );
            Pause();
            return;
        }

        if (user is null)
        {
            AnsiConsole.MarkupLine( "[red]User with specified email was not found.[/]" );
            Pause();
            return;
        }

        if (user.IsBlocked)
        {
            AnsiConsole.MarkupLine( "[yellow]User is already banned.[/]" );
            Pause();
            return;
        }

        if (user.Role == Role.Superadmin)
        {
            AnsiConsole.MarkupLine( "[red]Can't ban an superadmin![/]" );
            Pause();
            return;
        }

        if (!superadmin && user.Role == Role.Admin)
        {
            AnsiConsole.MarkupLine("[red]Can't ban an admin![/]");
            Pause();
            return;
        }

        bool confirm = AnsiConsole.Confirm( $"Ban user [yellow]{Markup.Escape(user.Email)}[/] (Id: [cyan]{user.Id}[/])?" );

        if (!confirm)
            return;

        try
        {
            await _requests.BlockUserAsync(user.Id);
            AnsiConsole.MarkupLine( "[green]User has been banned successfully.[/]" );
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Failed to ban user: {Markup.Escape(ex.Message)}[/]" );
        }

        Pause();
    }

    private protected async Task DoUnbanSubMenu(bool superadmin)
    {
        var email = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Enter user email to unban: [/]" )
                .Validate(input => !string.IsNullOrWhiteSpace(input) && input.Contains( "@" ) && input.Contains( "." ),
                    "[red]Please enter a valid email address[/]" ))
            .Trim();

        UserDto? user = null;
        try
        {
            user = await _requests.GetUserByEmailAsync(email);
        }
        catch (KeyNotFoundException)
        {
            AnsiConsole.MarkupLine( "[red]User with specified email was not found.[/]" );
            Pause();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Error while searching user: {Markup.Escape(ex.Message)}[/]" );
            Pause();
            return;
        }

        if (user is null)
        {
            AnsiConsole.MarkupLine( "[red]User with specified email was not found.[/]" );
            Pause();
            return;
        }

        if (!user.IsBlocked)
        {
            AnsiConsole.MarkupLine( "[yellow]User is not banned.[/]" );
            Pause();
            return;
        }

        if (user.Role == Role.Superadmin)
        {
            AnsiConsole.MarkupLine("[red]Can't unban an superadmin![/]");
            Pause();
            return;
        }

        if (!superadmin && user.Role == Role.Admin)
        {
            AnsiConsole.MarkupLine("[red]Can't unban an admin![/]");
            Pause();
            return;
        }

        bool confirm = AnsiConsole.Confirm( $"Unban user [yellow]{Markup.Escape(user.Email)}[/] (Id: [cyan]{user.Id}[/])?" );

        if (!confirm)
            return;

        try
        {
            await _requests.UnblockUserAsync(user.Id);
            AnsiConsole.MarkupLine( "[green]User has been unbanned successfully.[/]" );
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Failed to unban user: {Markup.Escape(ex.Message)}[/]" );
        }

        Pause();
    }

    private protected async Task DoDoctorRegistrationSubMenu()
    {
        UserRegisterDto? registerDto = await _registerHelper.RegisterPrompt();
        if (registerDto is null)
            return;

        DoctorRegisterDto doctorRegisterDto = _mapper.UserRegisterDtoToDoctorRegisterDto(registerDto);

        doctorRegisterDto = doctorRegisterDto with
        {
            Specialty = AnsiConsole.Prompt(
                new SelectionPrompt<Specialty>()
                    .Title( "[yellow]Select doctor's specialty:[/]" )
                    .AddChoices(Enum.GetValues<Specialty>())
            )
        };

        AuthResultDto? result = null;

        try
        {
            result = await _requests.CreateDoctorAsync(doctorRegisterDto);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Failed to register doctor: { Markup.Escape(ex.Message) }[/]" );
            Pause();
            return;
        }
        if (result is null)
        {
            AnsiConsole.MarkupLine( "[red]No result returned from registration.[/]" );
            Pause();
            return;
        }

        if (!result.Success)
        {
            AnsiConsole.MarkupLine( $"[red]Doctor registration failed: { Markup.Escape(result.Message ?? "Unknown error" ) }[/]" );
            Pause();
            return;
        }

        AnsiConsole.MarkupLine( "[green]Doctor registered successfully![/]" );
        Pause();
    }

    private protected async Task DoExportSubMenu()
    {
        var exportOptions = new[]
        {
            "Patients",
            "Doctors",
            "Users",
            "Appointments",
            "Audit Logs",
            "Exit"
        };

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[magenta]=== EXPORT CSV MENU ===[/]")
                .AddChoices(exportOptions)
        );

        (byte[] Content, string FileName)? exportResult = null;

        try
        {
            exportResult = choice switch
            {
                "Patients" => await _requests.ExportPatientsAsync(),
                "Doctors" => await _requests.ExportDoctorsAsync(),
                "Users" => await _requests.ExportUsersAsync(),
                "Appointments" => await _requests.ExportAppointmentsAsync(),
                "Audit Logs" => await _requests.ExportAuditAsync(),
                _ => null
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error during export: {Markup.Escape(ex.Message)}[/]");
            Pause();
            return;
        }

        if (exportResult.HasValue)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(exportResult.Value.FileName);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(exportResult.Value.FileName, exportResult.Value.Content);

                AnsiConsole.MarkupLine($"[green]Exported to file:[/] [yellow]{Markup.Escape(exportResult.Value.FileName)}[/]");
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine("[red]No permission to write file to the specified location.[/]");
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[red]I/O error while writing file: {Markup.Escape(ex.Message)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to save export file: {Markup.Escape(ex.Message)}[/]");
            }

            Pause();
        }
    }
}