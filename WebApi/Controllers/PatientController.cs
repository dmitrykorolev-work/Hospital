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
[Authorize(Roles = "Admin" )]
[ApiController]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly IAuditService _auditService;

    public PatientController(IPatientService patientService, IAuditService auditService)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // GET: api/Patient/{id}
    [HttpGet( "{id:guid}" )]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        var patient = await _patientService.GetByIdAsync(id).ConfigureAwait(false);
        if (patient is null) return NotFound();
        return Ok(patient);
    }

    // GET: api/Patient
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientDto>>> Search([FromQuery] PatientQueryDto query)
    {
        var result = await _patientService.SearchAsync(query).ConfigureAwait(false);
        return Ok(result);
    }

    // GET: api/Patient/export
    [HttpGet( "export" )]
    public async Task<IActionResult> Export([FromQuery] PatientQueryDto? query)
    {
        query ??= new PatientQueryDto();

        // For export, return all matching records: Page = 1, PageSize = int.MaxValue
        var q = query with { Page = 1, PageSize = int.MaxValue };

        var result = await _patientService.SearchAsync(q).ConfigureAwait(false);
        var patients = result.Items.ToList();

        // Using CsvHelper for correct CSV Serialization
        using var sw = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };

        using (var csv = new CsvWriter(sw, config))
        {
            // Specify format for DateTime
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "o" };

            csv.WriteHeader<PatientDto>();
            csv.NextRecord();

            await csv.WriteRecordsAsync(patients).ConfigureAwait(false);
        }

        var csvString = sw.ToString();
        var csvBytes = Encoding.UTF8.GetBytes(csvString);
        var fileName = $"patients_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        var actor = HttpContext.GetCurrentUserId();
        await SafeLogAsync(actor, AuditAct.Patient, $"Exported patients CSV. Count:{result.TotalCount}" ).ConfigureAwait(false);

        return File(csvBytes, "text/csv; charset=utf-8", fileName);
    }

    // POST: api/Patient/import
    [HttpPost( "import" )]
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

            IEnumerable<PatientDto> records;
            try
            {
                records = csv.GetRecords<PatientDto>().ToList();
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
                    PatientDto? existing = null;
                    if (dto.Id != Guid.Empty)
                    {
                        existing = await _patientService.GetByIdAsync(dto.Id).ConfigureAwait(false);
                    }
                    else
                    {
                        skipped++;
                        errors.Add( $"Row {processed}: Empty or invalid Id (Id: '{dto.Id}', Email: '{dto.Email ?? ""}')." );
                        continue;
                    }

                    if (existing is null)
                    {
                        skipped++;
                        errors.Add( $"Row {processed}: patient not found (Id: '{dto.Id}')." );
                        continue;
                    }

                    await _patientService.UpdateAsync(dto).ConfigureAwait(false);
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
            await SafeLogAsync(actorId, AuditAct.Patient, $"Import failed. Error: {ex.Message}" ).ConfigureAwait(false);
            throw;
        }

        await SafeLogAsync(actorId, AuditAct.Patient, $"Imported patients CSV. Processed: {processed} Updated: {updated} Skipped: {skipped}" ).ConfigureAwait(false);

        return Ok(new ImportResultDto(processed, updated, skipped, errors));
    }

    private async Task SafeLogAsync(Guid? userId, AuditAct action, string? details = null)
    {
        try
        {
            await _auditService.LogAsync(userId, action, details).ConfigureAwait(false);
        }
        catch
        {
            // Logging should not break API
        }
    }
}