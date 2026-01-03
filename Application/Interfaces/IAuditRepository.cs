using Hospital.Application.DTOs;
using Hospital.Domain.Entities;

namespace Hospital.Application.Interfaces;

public interface IAuditRepository
{
    Task<AuditLog?> GetByIdAsync(Guid id);
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<IEnumerable<AuditLog>> SearchAsync(AuditLogQueryDto query);
    Task AddAsync(AuditLog auditLog);
    Task<bool> DeleteAsync(Guid id);
}