using Microsoft.EntityFrameworkCore;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Persistence;

namespace Hospital.Infrastructure.Repositories;

public sealed class SqliteDoctorRepository : IDoctorRepository
{
    private readonly HospitalDbContext _context;

    public SqliteDoctorRepository(HospitalDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        return await _context.Doctors
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Doctor>> SearchAsync(DoctorQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var q = _context.Doctors.AsQueryable();

        // Name filter (first or last)
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var key = $"%{query.Name.Trim().ToLower()}%";
            q = q.Where(d =>
                EF.Functions.Like(d.FirstName.ToLower(), key) ||
                EF.Functions.Like(d.LastName.ToLower(), key));
        }

        // Phone filter
        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var phoneKey = query.Phone.Trim();
            q = q.Where(d => d.Phone.Contains(phoneKey));
        }

        // Email filter
        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var emailKey = query.Email.Trim().ToLower();
            q = q.Where(d => d.Email.ToLower().Contains(emailKey));
        }

        // Birth date range (compare by date)
        if (query.BirthDateFrom.HasValue)
        {
            var from = query.BirthDateFrom.Value.Date;
            q = q.Where(d => d.BirthDate >= from);
        }

        if (query.BirthDateTo.HasValue)
        {
            var to = query.BirthDateTo.Value.Date;

            // include whole day
            var toInclusive = to.AddDays(1).AddTicks(-1);
            q = q.Where(d => d.BirthDate <= toInclusive);
        }

        // Specialty filter
        if (query.Specialty.HasValue)
        {
            q = q.Where(d => d.Specialty == query.Specialty.Value);
        }

        return await q
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Doctor doctor)
    {
        await _context.Doctors.AddAsync(doctor).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        // Trying to find an entity that is already tracked or exists in the database
        var existing = await _context.Doctors.FindAsync(doctor.Id).ConfigureAwait(false);
        if (existing is null)
        {
            // If there is no entity in the context / DB - attach it and mark it as Modified
            _context.Doctors.Attach(doctor);
            _context.Entry(doctor).State = EntityState.Modified;
        }
        else
        {
            // If an entity is found / tracked - just update its values.
            _context.Entry(existing).CurrentValues.SetValues(doctor);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Doctors.FindAsync(id).ConfigureAwait(false);
        if (entity is null) return false;
        _context.Doctors.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<Doctor?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId)
            .ConfigureAwait(false);
    }
}