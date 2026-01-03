using Microsoft.EntityFrameworkCore;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Persistence;

namespace Hospital.Infrastructure.Repositories;

public sealed class SqlitePatientRepository : IPatientRepository
{
    private readonly HospitalDbContext _context;

    public SqlitePatientRepository(HospitalDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Patient>> GetAllAsync()
    {
        return await _context.Patients
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Patient>> SearchAsync(PatientQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var q = _context.Patients.AsQueryable();

        // Name filter (first or last)
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var key = $"%{query.Name.Trim().ToLower()}%";
            q = q.Where(p =>
                EF.Functions.Like(p.FirstName.ToLower(), key) ||
                EF.Functions.Like(p.LastName.ToLower(), key));
        }

        // Phone filter
        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var phoneKey = query.Phone.Trim();
            q = q.Where(p => p.Phone.Contains(phoneKey));
        }

        // Email filter
        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var emailKey = query.Email.Trim().ToLower();
            q = q.Where(p => p.Email.ToLower().Contains(emailKey));
        }

        // Birth date range (compare by date)
        if (query.BirthDateFrom.HasValue)
        {
            var from = query.BirthDateFrom.Value.Date;
            q = q.Where(p => p.BirthDate >= from);
        }

        if (query.BirthDateTo.HasValue)
        {
            var to = query.BirthDateTo.Value.Date;
            // include whole day
            var toInclusive = to.AddDays(1).AddTicks(-1);
            q = q.Where(p => p.BirthDate <= toInclusive);
        }

        return await q
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Patient patient)
    {
        await _context.Patients.AddAsync(patient).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Patient patient)
    {
        // Trying to find an entity that is already tracked or exists in the database
        var existing = await _context.Patients.FindAsync(patient.Id).ConfigureAwait(false);
        if (existing is null)
        {
            // If there is no entity in the context / DB - attach it and mark it as Modified
            _context.Patients.Attach(patient);
            _context.Entry(patient).State = EntityState.Modified;
        }
        else
        {
            // If an entity is found / tracked - just update its values.
            _context.Entry(existing).CurrentValues.SetValues(patient);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Patients.FindAsync(id).ConfigureAwait(false);
        if (entity is null) return false;
        _context.Patients.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<Patient?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId)
            .ConfigureAwait(false);
    }
}