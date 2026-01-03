using Hospital.Application.DTOs;
using Hospital.Domain.Enums;
using Hospital.ConsoleClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Hospital.ConsoleClient.Menu;

internal class MainMenu : IMenu
{
    private readonly IRequestsService _requests;
	private readonly IRegisterHelper _registerHelper;
	private readonly IServiceProvider _sp;

	public MainMenu(IRequestsService requests, IServiceProvider sp, IRegisterHelper registerHelper)
    {
        _requests = requests;
        _sp = sp;
        _registerHelper = registerHelper;

	}

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Clear();

            string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title( "[magenta]=== MEDICAL CENTER «HEALTH+» ===[/]" )
                .AddChoices(["Register", "Login", "Exit"])
                );

            AuthResultDto? authResult = null;

            switch (choice)
            {
                case "Register":
                    authResult = await DoRegisterAsync();
                    break;
                case "Login":
                    authResult = await DoLoginAsync();
                    break;
                case "Exit":
                    return;
                    //break;
            }

            if (authResult is not null && authResult.Success && authResult.Role.HasValue)
            {
                IMenu? menu = authResult.Role.Value switch
                {
                    Role.Admin => _sp.GetRequiredService<AdminMenu>(),
                    Role.Doctor => _sp.GetRequiredService<DoctorMenu>(),
                    Role.Patient => _sp.GetRequiredService<PatientMenu>(),
                    _ => null
                };

                if (menu is not null)
                {
                    await menu.RunAsync(cancellationToken);
                }
                else
                {
                    AnsiConsole.MarkupLine( "[red]Error: Unknown role![/]" );
                    AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
                    Console.ReadLine();
                }

            }
        }
    }

    private async Task<AuthResultDto?> DoLoginAsync()
    {
        var email = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Email: [/]" )
                .Validate(input => input.Contains( "@" ) && input.Contains( "." ),
                    "[red]Please enter a valid email address[/]" )
        );

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Password: [/]" )
                .Secret()
        );

        try
        {
            var result = await _requests.LoginAsync(new UserLoginDto(email, password));
            if (result is null || !result.Success)
            {
                AnsiConsole.MarkupLine( $"[red]Login failed: { Markup.Escape( result?.Message ?? "Unknown error" ) }[/]" );
                AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Login error: { Markup.Escape( ex.Message ?? "Unknown error" ) }[/]" );
            AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }
    }

    private async Task<AuthResultDto?> DoRegisterAsync()
    {
		UserRegisterDto? userRegisterDto = await _registerHelper.RegisterPrompt();
        if (userRegisterDto is null) return null;

		try
        {
            var result = await _requests.RegisterAsync(userRegisterDto);
            if (result is null || !result.Success)
            {
                AnsiConsole.MarkupLine( $"[red]Register failed: { Markup.Escape( result?.Message ?? "Unknown error" ) }[/]" );
                AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
                Console.ReadLine();
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Register error: { Markup.Escape( ex.Message ?? "Unknown error" ) }[/]" );
            AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }
    }
}