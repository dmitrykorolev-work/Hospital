using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record DoctorQueryDto(
    int Page = 1,
    int PageSize = 20,
    string? Name = null,
    string? Phone = null,
    string? Email = null,
    DateTime? BirthDateFrom = null,
    DateTime? BirthDateTo = null,
    string? SortBy = "LastName",
    string? SortDir = "asc",
    Specialty? Specialty = null
);