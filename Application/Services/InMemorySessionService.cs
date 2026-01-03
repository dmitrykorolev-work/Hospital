using System.Collections.Concurrent;
using Hospital.Application.Interfaces;
using Hospital.Domain.Entities;

namespace Hospital.Application.Services;

/// <summary>
/// Simple in-memory session token storage.
/// Singleton in DI - tokens live in process memory and are not written to the database.
/// </summary>
public sealed class InMemorySessionService : ISessionService
{
    private readonly ConcurrentDictionary<Guid, SessionToken> _store = new();

    public async Task<Guid> CreateSessionAsync(Guid userId, TimeSpan? ttl = null)
    {
        Guid token;

        // It's more likely to break due to a piano suddenly falling from the sky than from a repetition of a 128-bit key, but I don't want to rely on chance.
        do token = Guid.NewGuid();
        while (_store.ContainsKey(token));

        var now = DateTime.UtcNow;
        var expires = now.Add(ttl ?? TimeSpan.FromHours(24));

        var session = new SessionToken
        {
            Token = token,
            UserId = userId,
            CreatedAt = now,
            ExpiresAt = expires
        };

        _store[token] = session;
        return token;
    }

    public async Task<Guid?> ValidateSessionAsync(Guid token)
    {
        if (_store.TryGetValue(token, out var session))
        {
            if (session.ExpiresAt > DateTime.UtcNow)
            {
                return session.UserId;
            }

            // Expired - delete
            _store.TryRemove(token, out _);
        }

        return null;
    }

    public async Task RevokeSessionAsync(Guid token)
    {
        _store.TryRemove(token, out _);
    }

    public async Task<TimeSpan?> GetRemainingAsync(Guid token)
    {
        if (_store.TryGetValue(token, out var session))
        {
            var remaining = session.ExpiresAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                // Expired - delete
                _store.TryRemove(token, out _);
                return null;
            }

            return remaining;
        }

        return null;
    }
}