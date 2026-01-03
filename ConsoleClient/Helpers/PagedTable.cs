using Hospital.Application.DTOs;
using Hospital.ConsoleClient.Interfaces;
using Spectre.Console;
using System.Reflection;

namespace Hospital.ConsoleClient.Helpers;

public class PagedTable : IPagedTable
{
    public async Task<int?> ShowPagedTable<TDto>(PagedResult<TDto> data, int page = 1)
    {
        AnsiConsole.Clear();

        if (data is null)
        {
            AnsiConsole.MarkupLine( "[red]No data to display.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }

        if (data.Items is null)
        {
            AnsiConsole.MarkupLine( "[red]Received data contains no items.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }

        int totalPages = Math.Max((data.TotalCount + data.PageSize - 1) / data.PageSize, 1);

        string[] columns;
        string[][] rows;
        try
        {
            columns = typeof(TDto)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => p.Name)
                .ToArray();

            rows = data.Items.Select(v => columns.Select(col =>
            {
                var prop = typeof(TDto).GetProperty(col);
                try
                {
                    return prop?.GetValue(v)?.ToString() ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }).ToArray()).ToArray();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine( $"[red]Error forming table: {Markup.Escape(ex.Message)}[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }

        var table = new Table().Centered().RoundedBorder();
        table.ShowRowSeparators = true;

        // Add columns
        if (columns.Length == 0)
        {
            AnsiConsole.MarkupLine( "[red]No columns available to display.[/]" );
            AnsiConsole.MarkupLine( "[gray]Press <Enter> to continue[/]" );
            Console.ReadLine();
            return null;
        }

        table.AddColumns(columns);

        // Add rows
        foreach (var row in rows)
            table.AddRow(row);

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine( $"[grey]Page {page}/{totalPages} | ← prev | next → | Q: quit[/]" );

        // Reading key input
        while (true)
        {
            var keyInfo = AnsiConsole.Console.Input.ReadKey(true);

            if (!keyInfo.HasValue) return null;

            if (keyInfo.Value.Key == ConsoleKey.RightArrow)
            {
                if (page < totalPages) page++;
                return page;
            }
            else if (keyInfo.Value.Key == ConsoleKey.LeftArrow)
            {
                if (page > 1) page--;
                return page;
            }
            else if (keyInfo.Value.Key == ConsoleKey.Q || keyInfo.Value.Key == ConsoleKey.Escape)
            {
                return null;
            }
        }
    }
}
