using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Services;
    
public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly AppMapper _mapper;

    public AuditService(IAuditRepository auditRepository, AppMapper mapper)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task LogAsync(Guid? userId, AuditAct action, string? details = null)
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };

        // It's more likely to break due the sun going supernova specifically to spite this code than because of a repeated 128-bit key, but I don't want to rely on chance.
        do audit.Id = Guid.NewGuid();
        while (await _auditRepository.GetByIdAsync(audit.Id).ConfigureAwait(false) is not null);

        await _auditRepository.AddAsync(audit).ConfigureAwait(false);
    }

    public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
        {
            throw new ArgumentException( "From cannot be greater than To." );
        }

        var logs = (await _auditRepository.SearchAsync(query).ConfigureAwait(false)).AsQueryable();

        var sortBy = (query.SortBy ?? "Timestamp" ).Trim();
        var sortDir = (query.SortDir ?? "asc" ).Trim().ToLowerInvariant();

        logs = (sortBy, sortDir) switch
        {
            ( "userid", "asc" ) or ( "userId", "asc" ) => logs.OrderBy(a => a.UserId).ThenBy(a => a.Timestamp),
            ( "userid", "desc" ) or ( "userId", "desc" ) => logs.OrderByDescending(a => a.UserId).ThenByDescending(a => a.Timestamp),
            ( "action", "asc" ) => logs.OrderBy(a => a.Action).ThenBy(a => a.Timestamp),
            ( "action", "desc" ) => logs.OrderByDescending(a => a.Action).ThenByDescending(a => a.Timestamp),
            ( "timestamp", "asc" ) => logs.OrderBy(a => a.Timestamp),
            ( "timestamp", "desc" ) => logs.OrderByDescending(a => a.Timestamp),
            _ => logs.OrderBy(a => a.Timestamp),
        };

        var total = logs.Count();
        var items = logs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = items.Select(_mapper.AuditLogToAuditLogDto).ToList();

        return new PagedResult<AuditLogDto>(dtos, total, page, pageSize);
    }
}