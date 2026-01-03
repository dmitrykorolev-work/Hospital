using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record AuditLogQueryDto(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    DateTime? From = null,
    DateTime? To = null,
    AuditAct? Action = null,
    string? SortBy = "Timestamp",
    string? SortDir = "asc"
);
