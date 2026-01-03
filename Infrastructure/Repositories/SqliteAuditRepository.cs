using Microsoft.EntityFrameworkCore;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Persistence;

namespace Hospital.Infrastructure.Repositories;

public sealed class SqliteAuditRepository : IAuditRepository
{
    private readonly HospitalDbContext _context;

    public SqliteAuditRepository(HospitalDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<AuditLog>> SearchAsync(AuditLogQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var q = _context.AuditLogs.AsQueryable();

        if (query.UserId.HasValue)
        {
            q = q.Where(a => a.UserId == query.UserId.Value);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value;
            q = q.Where(a => a.Timestamp >= from);
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value.Date;

            // include whole day
            var toInclusive = to.AddDays(1).AddTicks(-1);
            q = q.Where(a => a.Timestamp <= toInclusive);
        }

        if (query.Action.HasValue)
        {
            q = q.Where(a => a.Action == query.Action.Value);
        }

        // Sorting
        var sortBy = (query.SortBy ?? "Timestamp" ).Trim().ToLowerInvariant();
        var sortDirAsc = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<AuditLog> ordered;
        switch (sortBy)
        {
            case "userid":
            case "userId":
                ordered = sortDirAsc ? q.OrderBy(a => a.UserId) : q.OrderByDescending(a => a.UserId);
                break;
            case "action":
                ordered = sortDirAsc ? q.OrderBy(a => a.Action) : q.OrderByDescending(a => a.Action);
                break;
            case "timestamp":
            default:
                ordered = sortDirAsc ? q.OrderBy(a => a.Timestamp) : q.OrderByDescending(a => a.Timestamp);
                break;
        }

        return await ordered
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(AuditLog auditLog)
    {
        if (auditLog is null) throw new ArgumentNullException(nameof(auditLog));
        await _context.AuditLogs.AddAsync(auditLog).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.AuditLogs.FindAsync(id).ConfigureAwait(false);
        if (entity is null) return false;
        _context.AuditLogs.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}