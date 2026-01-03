using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;
using Hospital.Application.Settings;
using Hospital.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Hospital.Application.Services;

public sealed class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly AppMapper _mapper;
    private readonly AppointmentOptions _options;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IPatientRepository patientRepository,
        AppMapper mapper,
        IOptions<AppointmentOptions> options)
    {
        _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
        _doctorRepository = doctorRepository ?? throw new ArgumentNullException(nameof(doctorRepository));
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));

        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<AppointmentDto?> GetByIdAsync(Guid id)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id).ConfigureAwait(false);
        return appointment is null ? null : _mapper.AppointmentToAppointmentDto(appointment);
    }

    public async Task<PagedResult<AppointmentDto>> SearchAsync(AppointmentQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
        {
            throw new ArgumentException( "From cannot be greater than To." );
        }

        var appointments = (await _appointmentRepository.SearchAsync(query).ConfigureAwait(false)).AsQueryable();

        var sortBy = (query.SortBy ?? "AppointmentTime" ).Trim();
        var sortDir = (query.SortDir ?? "asc" ).Trim().ToLowerInvariant();

        appointments = (sortBy, sortDir) switch
        {
            ( "AppointmentTime", "asc" ) => appointments.OrderBy(a => a.AppointmentTime),
            ( "AppointmentTime", "desc" ) => appointments.OrderByDescending(a => a.AppointmentTime),
            ( "Status", "asc" ) => appointments.OrderBy(a => a.Status).ThenBy(a => a.AppointmentTime),
            ( "Status", "desc" ) => appointments.OrderByDescending(a => a.Status).ThenBy(a => a.AppointmentTime),
            ( "DoctorId", "asc" ) => appointments.OrderBy(a => a.DoctorId).ThenBy(a => a.AppointmentTime),
            ( "DoctorId", "desc" ) => appointments.OrderByDescending(a => a.DoctorId).ThenBy(a => a.AppointmentTime),
            ( "PatientId", "asc" ) => appointments.OrderBy(a => a.PatientId).ThenBy(a => a.AppointmentTime),
            ( "PatientId", "desc" ) => appointments.OrderByDescending(a => a.PatientId).ThenBy(a => a.AppointmentTime),
            _ => appointments.OrderBy(a => a.AppointmentTime),
        };

        var total = appointments.Count();
        var items = appointments
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.AppointmentsToAppointmentDtos(items);

        return new PagedResult<AppointmentDto>(dtos, total, page, pageSize);
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        // General validation / checking doctor availability
        await ValidateAppointmentAsync(dto.AppointmentTime, dto.DoctorId, dto.PatientId, excludeAppointmentId: null).ConfigureAwait(false);

        var appointment = _mapper.CreateAppointmentDtoToAppointment(dto);

        // It's more likely to break due to a direct meteor hit than because of a repeated 128-bit key, but I don't want to rely on chance.
        do appointment.Id = Guid.NewGuid();
        while (await _appointmentRepository.GetByIdAsync(appointment.Id).ConfigureAwait(false) is not null);

        appointment.CreatedAt = DateTime.UtcNow;

        await _appointmentRepository.AddAsync(appointment).ConfigureAwait(false);

        return _mapper.AppointmentToAppointmentDto(appointment);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException( "id is required.", nameof(id));
        return await _appointmentRepository.DeleteAsync(id).ConfigureAwait(false);
    }

    public async Task<DoctorDto?> FindAvailableDoctorAsync(DateTime appointmentTime, Specialty specialty)
    {
        var session = TimeSpan.FromMinutes(_options.SessionDurationMinutes);
        var reqStart = appointmentTime;
        var reqEnd = reqStart.Add(session);

        var doctors = (await _doctorRepository.SearchAsync(new DoctorQueryDto(Specialty: specialty)).ConfigureAwait(false)).ToList();
        if (!doctors.Any()) return null;

        foreach (var doctor in doctors)
        {
            var appointments = await _appointmentRepository.GetByDoctorIdAsync(doctor.Id).ConfigureAwait(false);
            var hasConflict = appointments.Any(a =>
                a.Status == AppointmentStatus.Scheduled &&
                IntervalsOverlap(a.AppointmentTime, a.AppointmentTime.Add(session), reqStart, reqEnd));
            if (!hasConflict)
            {
                return _mapper.DoctorToDoctorDto(doctor);
            }
        }

        return null;
    }

    private static bool IntervalsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
    {
        // [start, end)
        return aStart < bEnd && bStart < aEnd;
    }

    private async Task ValidateAppointmentAsync(DateTime appointmentTime, Guid doctorId, Guid patientId, Guid? excludeAppointmentId)
    {
        if (patientId == Guid.Empty) throw new ArgumentException( "PatientId is required.", nameof(patientId));
        if (doctorId == Guid.Empty) throw new ArgumentException( "DoctorId is required.", nameof(doctorId));

        if (appointmentTime <= DateTime.UtcNow) throw new ArgumentException( "Сan't set an appointment in the past.", nameof(appointmentTime));

        // Checking the existence of patient / doctor
        var patient = await _patientRepository.GetByIdAsync(patientId).ConfigureAwait(false);
        if (patient is null) throw new KeyNotFoundException( "Patient not found." );

        var doctor = await _doctorRepository.GetByIdAsync(doctorId).ConfigureAwait(false);
        if (doctor is null) throw new KeyNotFoundException( "Doctor not found." );

        var session = TimeSpan.FromMinutes(_options.SessionDurationMinutes);
        var requestedStart = appointmentTime;
        var requestedEnd = requestedStart.Add(session);

        if ((requestedStart.Hour < _options.OpeningTime || requestedStart.Hour >= _options.ClosingTime)
             || (requestedEnd.Hour < _options.OpeningTime || requestedEnd.Hour >= _options.ClosingTime))
            throw new ArgumentException( "Сan't set an appointment outside of working hours.", nameof(appointmentTime));

        // Check doctor availability: When updating, exclude the current appointment by ID
        var doctorAppointments = await _appointmentRepository.GetByDoctorIdAsync(doctorId).ConfigureAwait(false);
        
        var conflict = doctorAppointments.Any(a =>
            (excludeAppointmentId is null || a.Id != excludeAppointmentId.Value) &&
            a.Status == AppointmentStatus.Scheduled &&
            IntervalsOverlap(a.AppointmentTime, a.AppointmentTime.Add(session), requestedStart, requestedEnd));
        
        if (conflict) throw new InvalidOperationException( "Doctor is not available at requested time." );
    }

    public async Task CloseAsync(Guid appointmentId, Guid doctorId, string? doctorNotes)
    {
        if (appointmentId == Guid.Empty) throw new ArgumentException( "appointmentId is required.", nameof(appointmentId));
        if (doctorId == Guid.Empty) throw new ArgumentException( "doctorId is required.", nameof(doctorId));

        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId).ConfigureAwait(false);
        if (appointment is null) throw new KeyNotFoundException( "Appointment not found." );

        if (appointment.DoctorId != doctorId) throw new InvalidOperationException( "Doctor is not owner of this appointment." );
        if (appointment.Status != AppointmentStatus.Scheduled) throw new InvalidOperationException( "Only scheduled appointments can be closed." );

        appointment.Status = AppointmentStatus.Completed;
        appointment.DoctorNotes = doctorNotes;

        await _appointmentRepository.UpdateAsync(appointment).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AppointmentDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.Id == Guid.Empty) throw new ArgumentException( "Id is required.", nameof(dto.Id));

        var existing = await _appointmentRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (existing is null) throw new KeyNotFoundException( "Appointment not found." );

        //await ValidateAppointmentAsync(dto.AppointmentTime, dto.DoctorId, dto.PatientId, excludeAppointmentId: existing.Id).ConfigureAwait(false);

        existing.PatientId = dto.PatientId;
        existing.DoctorId = dto.DoctorId;
        existing.AppointmentTime = dto.AppointmentTime;
        existing.Notes = dto.Notes;
        existing.DoctorNotes = dto.DoctorNotes;
        existing.Status = dto.Status;
        // CreatedAt and Id are not changed

        await _appointmentRepository.UpdateAsync(existing).ConfigureAwait(false);
    }
}