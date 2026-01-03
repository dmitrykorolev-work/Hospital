namespace Hospital.Application.DTOs;

public record ReportResultDto(
    int TotalPatients,
    int TotalAppointments,
    double AverageAge,
    DateTime GeneratedAt
);
