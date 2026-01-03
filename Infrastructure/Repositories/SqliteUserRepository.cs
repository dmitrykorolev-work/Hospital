using Microsoft.EntityFrameworkCore;
using Hospital.Application.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Persistence;

namespace Hospital.Infrastructure.Repositories;

public sealed class SqliteUserRepository : IUserRepository
{
    private readonly HospitalDbContext _context;

    public SqliteUserRepository(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(User user)
    {
        // Trying to find an entity that is already tracked or exists in the database
        var existing = await _context.Users.FindAsync(user.Id).ConfigureAwait(false);
        if (existing is null)
        {
            // If there is no entity in the context / DB - attach it and mark it as Modified
            _context.Users.Attach(user);
            _context.Entry(user).State = EntityState.Modified;
        }
        else
        {
            // If an entity is found / tracked - just update its values.
            _context.Entry(existing).CurrentValues.SetValues(user);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Users.FindAsync(id).ConfigureAwait(false);
        if (entity is null) return false;
        _context.Users.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}