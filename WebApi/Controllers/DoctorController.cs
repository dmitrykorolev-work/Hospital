using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;
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
public class DoctorController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly IAuthService _authService;
    private readonly AppMapper _mapper;
    private readonly IAuditService _auditService;

    public DoctorController(IDoctorService doctorService, IAuthService authService, AppMapper mapper, IAuditService auditService)
    {
        _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // GET: api/Doctor/{id}
    [HttpGet( "{id:guid}" )]
    public async Task<ActionResult<DoctorDto>> GetById(Guid id)
    {
        var doctor = await _doctorService.GetByIdAsync(id).ConfigureAwait(false);
        if (doctor is null) return NotFound();

        var userId = HttpContext.GetCurrentUserId();
        //await SafeLogAsync(userId, AuditAct.Doctor, $"Viewed doctor. DoctorId: {id}" );

        return Ok(doctor);
    }

    // GET: api/Doctor
    [HttpGet]
    public async Task<ActionResult<PagedResult<DoctorDto>>> Search([FromQuery] DoctorQueryDto query)
    {
        var result = await _doctorService.SearchAsync(query).ConfigureAwait(false);

        var userId = HttpContext.GetCurrentUserId();
        //await SafeLogAsync(userId, AuditAct.Doctor, $"Searched doctors. Specialty: {query?.Specialty} Page: {query?.Page} PageSize: {query?.PageSize}" );

        return Ok(result);
    }

    // GET: api/Doctor/export
    [HttpGet( "export" )]
    public async Task<IActionResult> Export([FromQuery] DoctorQueryDto? query)
    {
        query ??= new DoctorQueryDto();

        // For export, return all matching records: Page = 1, PageSize = int.MaxValue
        var q = query with { Page = 1, PageSize = int.MaxValue };

        var result = await _doctorService.SearchAsync(q).ConfigureAwait(false);
        var doctors = result.Items.ToList();

        // Using CsvHelper for correct CSV Serialization
        using var sw = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };

        using (var csv = new CsvWriter(sw, config))
        {
            // Specify format for DateTime
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "o" };

            csv.WriteHeader<DoctorDto>();
            csv.NextRecord();

            await csv.WriteRecordsAsync(doctors).ConfigureAwait(false);
        }

        var csvString = sw.ToString();
        var csvBytes = Encoding.UTF8.GetBytes(csvString);
        var fileName = $"doctors_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        await SafeLogAsync(HttpContext.GetCurrentUserId(), AuditAct.Doctor, $"Exported doctors CSV. Count: {result.TotalCount}" ).ConfigureAwait(false);

        return File(csvBytes, "text/csv; charset=utf-8", fileName);
    }

    // POST: api/Doctor/import
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

            IEnumerable<DoctorDto> records;
            try
            {
                records = csv.GetRecords<DoctorDto>().ToList();
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
                    DoctorDto? existing = null;
                    if (dto.Id != Guid.Empty)
                    {
                        existing = await _doctorService.GetByIdAsync(dto.Id).ConfigureAwait(false);
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
                        errors.Add( $"Row {processed}: doctor not found (Id: '{dto.Id}')." );
                        continue;
                    }

                    await _doctorService.UpdateAsync(dto).ConfigureAwait(false);
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
            await SafeLogAsync(actorId, AuditAct.Doctor, $"Import failed. Error: {ex.Message}" ).ConfigureAwait(false);
            throw;
        }

        await SafeLogAsync(actorId, AuditAct.Doctor, $"Imported doctors CSV. Processed: {processed} Updated: {updated} Skipped: {skipped}" ).ConfigureAwait(false);

        return Ok( new ImportResultDto(processed, updated, skipped, errors) );
    }

    // POST: api/Doctor/{id}

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DoctorRegisterDto dto)
    {
        if (dto is null) return BadRequest();

        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            UserRegisterDto userRegisterDto = _mapper.DoctorRegisterDtoToUserRegisterDto(dto);

            var result = await _authService.RegisterAsync(userRegisterDto, Role.Doctor, dto.Specialty).ConfigureAwait(false);

            var userId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(userId, AuditAct.Doctor, $"Create doctor attempt. Email: {dto?.Email} Success: {result.Success}"
                               + (result.Message is not null ? $" Message: {result.Message}" : "" )
            );

            if (!result.Success)
            {
                // Simulate delay to prevent brute-force attacks
                await Task.Delay(TimeSpan.FromSeconds(3), HttpContext.RequestAborted).ConfigureAwait(false);
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            var userId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(userId, AuditAct.Doctor, $"Create doctor failed - bad request: {ex.Message} Email: {dto?.Email}" );
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            var userId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(userId, AuditAct.Doctor, $"Create doctor failed - conflict: {ex.Message} Email: {dto?.Email}" );
            return Conflict(ex.Message);
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
            // Logging should not break API
        }
    }
}