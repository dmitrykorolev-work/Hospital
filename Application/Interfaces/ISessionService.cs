namespace Hospital.Application.Interfaces;

public interface ISessionService
{
    Task<Guid> CreateSessionAsync(Guid userId, TimeSpan? ttl = null);
    Task<Guid?> ValidateSessionAsync(Guid token);
    Task RevokeSessionAsync(Guid token);
    Task<TimeSpan?> GetRemainingAsync(Guid token);
}
