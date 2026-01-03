using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Repositories;

public sealed class SqliteAppointmentRepository : IAppointmentRepository
{
    private readonly HospitalDbContext _context;

    public SqliteAppointmentRepository(HospitalDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        return await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Appointment>> GetAllAsync()
    {
        return await _context.Appointments
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Appointment>> SearchAsync(AppointmentQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var q = _context.Appointments.AsQueryable();

        if (query.DoctorId.HasValue)
        {
            var doctorId = query.DoctorId.Value;
            q = q.Where(a => a.DoctorId == doctorId);
        }

        if (query.PatientId.HasValue)
        {
            var patientId = query.PatientId.Value;
            q = q.Where(a => a.PatientId == patientId);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value;
            q = q.Where(a => a.AppointmentTime >= from);
        }

        if (query.To.HasValue)
        {
            // Include whole day for date-only intent
            var toInclusive = query.To.Value.AddDays(1).AddTicks(-1);
            q = q.Where(a => a.AppointmentTime <= toInclusive);
        }

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            q = q.Where(a => a.Status == status);
        }

        // Sorting: Support a limited set of fields, defaulting to AppointmentTime asc
        var sortBy = (query.SortBy ?? "AppointmentTime" ).Trim();
        var sortDir = (query.SortDir ?? "asc" ).Trim().ToLowerInvariant();

        q = (sortBy, sortDir) switch
        {
            ( "AppointmentTime", "asc" ) => q.OrderBy(a => a.AppointmentTime),
            ( "AppointmentTime", "desc" ) => q.OrderByDescending(a => a.AppointmentTime),
            ( "Status", "asc" ) => q.OrderBy(a => a.Status).ThenBy(a => a.AppointmentTime),
            ( "Status", "desc" ) => q.OrderByDescending(a => a.Status).ThenBy(a => a.AppointmentTime),
            ( "DoctorId", "asc" ) => q.OrderBy(a => a.DoctorId).ThenBy(a => a.AppointmentTime),
            ( "DoctorId", "desc" ) => q.OrderByDescending(a => a.DoctorId).ThenBy(a => a.AppointmentTime),
            ( "PatientId", "asc" ) => q.OrderBy(a => a.PatientId).ThenBy(a => a.AppointmentTime),
            ( "PatientId", "desc" ) => q.OrderByDescending(a => a.PatientId).ThenBy(a => a.AppointmentTime),
            _ => q.OrderBy(a => a.AppointmentTime),
        };

        return await q
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.AppointmentTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(Guid doctorId)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId)
            .OrderBy(a => a.AppointmentTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Appointment appointment)
    {
        await _context.Appointments.AddAsync(appointment).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        if (appointment is null) throw new ArgumentNullException(nameof(appointment));

        // Trying to find an entity that is already tracked or exists in the database
        var existing = await _context.Appointments.FindAsync(appointment.Id).ConfigureAwait(false);

        if (existing is null)
        {
            // If there is no entity in the context / DB - attach it and mark it as Modified
            _context.Appointments.Attach(appointment);
            _context.Entry(appointment).State = EntityState.Modified;
        }
        else
        {
            // If an entity is found / tracked - just update its values.
            _context.Entry(existing).CurrentValues.SetValues(appointment);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Appointments.FindAsync(id).ConfigureAwait(false);
        if (entity is null) return false;
        _context.Appointments.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}