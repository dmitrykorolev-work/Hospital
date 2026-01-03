namespace Hospital.ConsoleClient.Interfaces;

internal interface IMenu
{
    Task RunAsync( CancellationToken cancellationToken = default );
}