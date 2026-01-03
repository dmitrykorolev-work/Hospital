namespace Hospital.Application.DTOs;

public record ImportResultDto (
    int Processed,
    int Updated,
    int Skipped,
    IEnumerable<string> Errors
);