namespace Hospital.Application.Settings;

/// <summary>
/// Appointment settings.
/// Values ​​are read from the "AppointmentOptions" section in appsettings.json.
/// </summary>
public class AppointmentOptions
{
    public int SessionDurationMinutes { get; set; } = 60;

    public int OpeningTime { get; set; } = 10;
    public int ClosingTime { get; set; } = 20;
}