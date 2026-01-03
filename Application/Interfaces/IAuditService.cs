using Hospital.Application.DTOs;
using Hospital.Domain.Enums;

namespace Hospital.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid? userId, AuditAct action, string? details = null);

    Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogQueryDto query);
}
