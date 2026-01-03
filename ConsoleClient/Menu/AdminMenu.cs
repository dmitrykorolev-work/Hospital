using Hospital.Application.DTOs;
using Hospital.Application.Mappings;
using Hospital.ConsoleClient.Interfaces;
using Hospital.Domain.Enums;
using Spectre.Console;
using System.Reflection;

namespace Hospital.ConsoleClient.Menu;

internal class AdminMenu : IMenu // TODO: Separate into multiple files? (600+ lines of code omg)
{
    private readonly IRequestsService _requests;
    private readonly IPagedTable _pagedTable;
    private readonly IRegisterHelper _registerHelper;
    private readonly AppMapper _mapper;

    public AdminMenu(IRequestsService requests, IPagedTable pagedTable, IRegisterHelper registerHelper, AppMapper mapper)
    {
        _requests = requests;
        _pagedTable = pagedTable;
        _registerHelper = registerHelper;
        _mapper = mapper;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Clear();

            string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title( "[magenta]=== ADMIN MENU ===[/]" )
                .AddChoices( ["Patients", "Doctors", "Users", "Audit", "Report", "Ban user", "Unban user", "Doctor registration", "Export CSV", "Import CSV", "Exit"] )
                .PageSize(15)
                );

            await (choice switch
            {
                "Patients" => DoPatientsSubMenu(),
                "Doctors" => DoDoctorsSubMenu(),
                "Users" => DoUsersSubMenu(),
                "Audit" => DoAuditSubMenu(),
                "Report" => DoReportSubMenu(),
                "Ban user" => DoBanSubMenu(),
                "Unban user" => DoUnbanSubMenu(),
                "Doctor registration" => DoDoctorRegistrationSubMenu(),
                "Export CSV" => DoExportSubMenu(),
                "Import CSV" => DoImportSubMenu(),
                "Exit" => Task.CompletedTask,
                _ => Task.CompletedTask
            });

            if (choice == "Exit" )
                return;
        }
    }

    private async Task DoPatientsSubMenu()
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
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve patients: { Markup.Escape(ex.Message) }[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving patients.[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private async Task DoDoctorsSubMenu()
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
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve doctors: { Markup.Escape(ex.Message) }[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving doctors.[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private async Task DoUsersSubMenu()
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
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve users: { Markup.Escape(ex.Message) }[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving users.[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private async Task DoAuditSubMenu()
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
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine( $"[red]Failed to retrieve audit logs: { Markup.Escape(ex.Message) }[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            if (data is null)
            {
                AnsiConsole.MarkupLine( "[red]Server returned empty response while retrieving audit logs.[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return;
            }

            page = await _pagedTable.ShowPagedTable(data, page.Value);
        }
    }

    private async Task DoReportSubMenu()
    {
        ReportResultDto? result = null;

        try
        {
            result = await _requests.GenerateReportAsync( new ReportRequestDto() );
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine( "[red]Operation cancelled.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Failed to generate report: { Markup.Escape(ex.Message) }[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (result is null)
        {
            AnsiConsole.MarkupLine( "[red]Server returned empty report.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
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

    private async Task DoExportSubMenu()
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
                .Title( "[magenta]=== EXPORT CSV MENU ===[/]" )
                .AddChoices( exportOptions )
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
            AnsiConsole.MarkupLine( $"[red]Error during export: { Markup.Escape(ex.Message) }[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (exportResult.HasValue)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName( exportResult.Value.FileName );
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                System.IO.File.WriteAllBytes(exportResult.Value.FileName, exportResult.Value.Content);

                AnsiConsole.MarkupLine( $"[green]Exported to file:[/] [yellow]{ Markup.Escape( exportResult.Value.FileName ) }[/]" );
                AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine( "[red]No permission to write file to the specified location.[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine( $"[red]I/O error while writing file: { Markup.Escape(ex.Message) }[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine( $"[red]Failed to save export file: { Markup.Escape(ex.Message) }[/]" );
                AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
            }
        }
    }

    private async Task DoImportSubMenu()
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
                .Title( "[magenta]=== IMPORT CSV MENU ===[/]" )
                .AddChoices(importOptions)
        );

        if (choice == "Exit" )
            return;

        // Ask for file path
        string filePath = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]File path: [/]" )
                .Validate(input => !string.IsNullOrWhiteSpace(input) && File.Exists(input),
                    "[red]Please enter a valid file path[/]" ));

        byte[] content;

        try
        {
            content = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Failed to read file: { Markup.Escape(ex.Message) }[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
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
            AnsiConsole.MarkupLine( "[red]Import failed: target resource not found (404).[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine( "[red]Operation cancelled.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Import failed: { Markup.Escape(ex.Message) }[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (result is null)
        {
            AnsiConsole.MarkupLine( "[red]No result returned from import.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        AnsiConsole.MarkupLine( "[green]Import summary:[/]" );
        AnsiConsole.MarkupLine( $"  [yellow]Processed:[/] { result.Processed }" );
        AnsiConsole.MarkupLine( $"  [yellow]Updated:[/] { result.Updated }" );
        AnsiConsole.MarkupLine( $"  [yellow]Skipped:[/] { result.Skipped }" );

        if ( result.Errors != null && result.Errors.Any() )
        {
            AnsiConsole.MarkupLine( "[red]Errors:[/]" );
            foreach ( var err in result.Errors )
            {
                AnsiConsole.MarkupLine( $"  - { Markup.Escape(err) }" );
            }
        }

        AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
        Console.ReadLine();
    }

    private async Task DoBanSubMenu()
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
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Error while searching user: {Markup.Escape(ex.Message)}[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (user is null)
        {
            AnsiConsole.MarkupLine( "[red]User with specified email was not found.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (user.IsBlocked)
        {
            AnsiConsole.MarkupLine( "[yellow]User is already banned.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (user.Role == Role.Admin)
        {
            AnsiConsole.MarkupLine( "[red]Can't ban an admin![/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
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

        AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
        Console.ReadLine();
    }

    private async Task DoUnbanSubMenu()
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
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Error while searching user: {Markup.Escape(ex.Message)}[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (user is null)
        {
            AnsiConsole.MarkupLine( "[red]User with specified email was not found.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (!user.IsBlocked)
        {
            AnsiConsole.MarkupLine( "[yellow]User is not banned.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (user.Role == Role.Admin)
        {
            AnsiConsole.MarkupLine( "[red]Can't unban an admin![/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
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

        AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
        Console.ReadLine();
    }

    private async Task DoDoctorRegistrationSubMenu()
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
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }
        if (result is null)
        {
            AnsiConsole.MarkupLine( "[red]No result returned from registration.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        if (!result.Success)
        {
            AnsiConsole.MarkupLine( $"[red]Doctor registration failed: { Markup.Escape(result.Message ?? "Unknown error" ) }[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return;
        }

        AnsiConsole.MarkupLine( "[green]Doctor registered successfully![/]" );
        AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
        Console.ReadLine();
    }
}