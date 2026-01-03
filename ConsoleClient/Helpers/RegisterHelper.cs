using Hospital.Application.DTOs;
using Hospital.ConsoleClient.Interfaces;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Hospital.ConsoleClient.Helpers;

internal class RegisterHelper : IRegisterHelper
{
    public async Task<UserRegisterDto?> RegisterPrompt()
    {
        var email = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Email: [/]" )
                .Validate(input => input.Contains( "@" ) && input.Contains( "." ),
                    "[red]Please enter a valid email address[/]" )
        );

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Password: [/]" )
                .Secret()
                .Validate(value => value.Length > 8, "[red]Password must be at least 8 characters long![/]" )
        );

        var passwordn = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Retype password: [/]" )
                .Secret()
        );

        if (password != passwordn)
        {
            AnsiConsole.MarkupLine( "[red]Passwords do not match[/]" );
            AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }

        var firstName = AnsiConsole.Prompt(new TextPrompt<string>( "[yellow]FirstName: [/]" ));
        var lastName = AnsiConsole.Prompt(new TextPrompt<string>( "[yellow]LastName: [/]" ));

        var phone = AnsiConsole.Prompt(
            new TextPrompt<string>( "[yellow]Phone: [/]" )
                .Validate(input =>
                {
                    var re = new Regex(@"^\+?[1-9]\d{1,14}$" );
                    return !string.IsNullOrWhiteSpace(input) && re.IsMatch(input)
                        ? ValidationResult.Success()
                        : ValidationResult.Error( "[red]Invalid phone number! Example: +380123456789[/]" );
                })
        );

        var birthDate = AnsiConsole.Prompt(
            new TextPrompt<DateTime>( "[yellow]Birth date (YYYY-MM-DD): [/]" )
        );

        if (birthDate >= DateTime.Now)
        {
            AnsiConsole.MarkupLine( "[red]Birth date must be in the past.[/]" ); // Come back when you're born!
            AnsiConsole.MarkupLine( $"[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }

        return new UserRegisterDto
        (
            email,
            password,
            firstName,
            lastName,
            phone,
            birthDate
        );
    }
}
