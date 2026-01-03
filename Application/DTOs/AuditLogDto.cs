using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    DateTime Timestamp,
    AuditAct Action,
    string Details
);
