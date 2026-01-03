using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Enums;
using Hospital.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Hospital.WebApi.Controllers;

[Route( "api/[controller]" )]
[ApiController]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IAuditService _auditService;

    public AppointmentController(
        IAppointmentService appointmentService,
        IPatientService patientService,
        IDoctorService doctorService,
        IAuditService auditService)
    {
        _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // POST: api/Appointment/book
    [HttpPost( "book" )]
    [Authorize(Roles = "Patient" )]
    public async Task<IActionResult> Book([FromBody] BookAppointmentRequest dto)
    {
        if (dto is null) return BadRequest();

        var userId = HttpContext.GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var patient = await _patientService.GetByUserIdAsync(userId.Value).ConfigureAwait(false);
        if (patient is null) return NotFound( "Patient profile not found." );

        var doctor = await _appointmentService.FindAvailableDoctorAsync(dto.AppointmentTime, dto.Specialty).ConfigureAwait(false);
        if (doctor is null)
        {
            await SafeLogAsync(userId, AuditAct.Apponiment, $"Booking failed - no available doctor. PatientId:{patient.Id} Time:{dto.AppointmentTime:o}" );
            return Conflict(new { Message = "No available doctor for requested time and specialty." });
        }

        var createDto = new CreateAppointmentDto(patient.Id, doctor.Id, dto.AppointmentTime, dto.Notes);

        try
        {
            var appointment = await _appointmentService.CreateAsync(createDto).ConfigureAwait(false);

            await SafeLogAsync(userId, AuditAct.Apponiment, $"Booked AppointmentId:{appointment.Id} PatientId:{patient.Id} DoctorId:{doctor.Id} Time:{appointment.AppointmentTime:o}" );

            return Ok(new AppointmentBookResultDto(
                true,
                "Appointment created successfully.",
                appointment.Id,
                doctor.FirstName,
                doctor.LastName
            ));
        }
        catch (ArgumentException ex)
        {
            await SafeLogAsync(userId, AuditAct.Apponiment, $"Booking failed - bad request: {ex.Message} PatientId: {patient.Id} DoctorId: {doctor?.Id} Time:{dto.AppointmentTime:o}" );
            return BadRequest(new AppointmentBookResultDto(
                false,
                ex.Message,
                null,
                null,
                null
            ));
        }
        catch (KeyNotFoundException ex)
        {
            await SafeLogAsync(userId, AuditAct.Apponiment, $"Booking failed - not found: {ex.Message} PatientId: {patient.Id} DoctorId: {doctor?.Id} Time: {dto.AppointmentTime:o}" );
            return NotFound(new AppointmentBookResultDto(
                false,
                ex.Message,
                null,
                null,
                null
            ));
        }
        catch (InvalidOperationException ex)
        {
            await SafeLogAsync(userId, AuditAct.Apponiment, $"Booking conflict: {ex.Message} PatientId: {patient.Id} DoctorId: {doctor?.Id} Time: {dto.AppointmentTime:o}" );
            return Conflict(new AppointmentBookResultDto(
                false,
                ex.Message,
                null,
                null,
                null
            ));
        }
    }

    // GET: api/Appointment/patient
    [HttpGet( "patient" )]
    [Authorize(Roles = "Patient" )]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> GetForPatient([FromQuery] AppointmentQueryDto? query)
    {
        var userId = HttpContext.GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var patient = await _patientService.GetByUserIdAsync(userId.Value).ConfigureAwait(false);
        if (patient is null) return NotFound( "Patient profile not found." );

        query ??= new AppointmentQueryDto();
        var q = query with { PatientId = patient.Id };

        var result = await _appointmentService.SearchAsync(q).ConfigureAwait(false);

        //await SafeLogAsync(userId, AuditAct.Patient, $"Viewed appointments for patient. PatientId:{patient.Id} QueryPage:{query.Page} PageSize:{query.PageSize}" );

        return Ok(result);
    }

    // GET: api/Appointment/doctor
    [HttpGet( "doctor" )]
    [Authorize(Roles = "Doctor" )]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> GetForDoctor([FromQuery] AppointmentQueryDto? query)
    {
        var userId = HttpContext.GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var doctor = await _doctorService.GetByUserIdAsync(userId.Value).ConfigureAwait(false);
        if (doctor is null) return NotFound( "Doctor profile not found." );

        query ??= new AppointmentQueryDto();
        var q = query with { DoctorId = doctor.Id };

        var result = await _appointmentService.SearchAsync(q).ConfigureAwait(false);

        //await SafeLogAsync(userId, AuditAct.Doctor, $"Viewed appointments for doctor. DoctorId:{doctor.Id} QueryPage:{query.Page} PageSize:{query.PageSize}" );

        return Ok(result);
    }

    // GET: api/Appointment/export
    [HttpGet( "export" )]
    [Authorize(Roles = "Admin" )]
    public async Task<IActionResult> Export([FromQuery] AppointmentQueryDto? query)
    {
        var userId = HttpContext.GetCurrentUserId();
        if (userId is null) return Unauthorized();

        query ??= new AppointmentQueryDto();

        // For export, return all matching records: Page = 1, PageSize = int.MaxValue
        var q = query with { Page = 1, PageSize = int.MaxValue };

        var result = await _appointmentService.SearchAsync(q).ConfigureAwait(false);
        var appointments = result.Items.ToList();

        // Using CsvHelper for correct CSV Serialization
        using var sw = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };

        using (var csv = new CsvWriter(sw, config))
        {
            // Specify format for DateTime
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "o" };

            csv.WriteHeader<AppointmentDto>();
            csv.NextRecord();

            await csv.WriteRecordsAsync(appointments).ConfigureAwait(false);
        }

        var csvString = sw.ToString();
        var csvBytes = Encoding.UTF8.GetBytes(csvString);
        var fileName = $"appointments_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        await SafeLogAsync(userId, AuditAct.Apponiment, $"Exported appointments CSV. Count: {result.TotalCount} ByUser: {userId}" ).ConfigureAwait(false);

        return File(csvBytes, "text/csv; charset=utf-8", fileName);
    }

    // POST: api/Appointment/import
    [HttpPost( "import" )]
    [Authorize(Roles = "Admin" )]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest( "CSV file is required." );
        }

        var processed = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();

        var actorId = HttpContext.GetCurrentUserId();

        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                PrepareHeaderForMatch = args => args.Header?.Trim()
            };

            using var csv = new CsvReader(reader, config);

            // Specify format for DateTime
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "o" };

            IEnumerable<AppointmentDto> records;
            try
            {
                records = csv.GetRecords<AppointmentDto>().ToList();
            }
            catch (Exception ex)
            {
                return BadRequest( $"Failed to parse CSV: {ex.Message}" );
            }

            foreach (var dto in records)
            {
                processed++;
                try
                {
                    AppointmentDto? existing = null;

                    if (dto.Id != Guid.Empty)
                    {
                        existing = await _appointmentService.GetByIdAsync(dto.Id).ConfigureAwait(false);
                    }
                    else
                    {
                        skipped++;
                        errors.Add( $"Row {processed}: Empty or invalid Id (Id: '{dto.Id}', Time: '{dto.AppointmentTime:o}')." );
                        continue;
                    }

                    if (existing is null)
                    {
                        skipped++;
                        errors.Add( $"Row {processed}: appointment not found (Id: '{dto.Id}')." );
                        continue;
                    }

                    await _appointmentService.UpdateAsync(dto).ConfigureAwait(false);
                    updated++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    errors.Add( $"Row {processed}: update failed. Error: {ex.Message}" );
                }
            }
        }
        catch (Exception ex)
        {
            await SafeLogAsync(actorId, AuditAct.Apponiment, $"Import failed. Error: {ex.Message}" ).ConfigureAwait(false);
            throw;
        }

        await SafeLogAsync(actorId, AuditAct.Apponiment, $"Imported appointments CSV. Processed: {processed} Updated: {updated} Skipped: {skipped}" ).ConfigureAwait(false);

        return Ok(new ImportResultDto(processed, updated, skipped, errors));
    }

    // POST: api/Appointment/{id}/close
    [HttpPost( "{id:guid}/close" )]
    [Authorize(Roles = "Doctor" )]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseAppointmentRequest dto)
    {
        if (id == Guid.Empty) return BadRequest();

        var userId = HttpContext.GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var doctor = await _doctorService.GetByUserIdAsync(userId.Value).ConfigureAwait(false);
        if (doctor is null) return NotFound( "Doctor profile not found." );

        try
        {
            await _appointmentService.CloseAsync(id, doctor.Id, dto?.DoctorNotes).ConfigureAwait(false);

            await SafeLogAsync(userId, AuditAct.Apponiment, $"Closed AppointmentId: {id} DoctorId: {doctor.Id} Notes: {dto?.DoctorNotes}" );

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            await SafeLogAsync(userId, AuditAct.Apponiment, $"Close failed - not found. AppointmentId: {id} DoctorId: {doctor.Id}" );
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            await SafeLogAsync(userId, AuditAct.Apponiment, $"Close failed - invalid operation: {ex.Message} AppointmentId: {id} DoctorId: {doctor.Id}" );
            return BadRequest(ex.Message);
        }
    }

    private async Task SafeLogAsync(Guid? userId, AuditAct action, string? details = null)
    {
        try
        {
            await _auditService.LogAsync(userId, action, details).ConfigureAwait(false);
        }
        catch
        {
            // Logging must not break API
        }
    }
}