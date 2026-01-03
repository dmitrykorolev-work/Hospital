namespace Hospital.Application.DTOs;

public record PatientQueryDto(
    int Page = 1,
    int PageSize = 20,
    string? Name = null,           // Any part of full name (first/last)
    string? Phone = null,
    string? Email = null,
    DateTime? BirthDateFrom = null,
    DateTime? BirthDateTo = null,
    string? SortBy = "LastName",
    string? SortDir = "asc"
);