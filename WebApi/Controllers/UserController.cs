using CsvHelper;
using CsvHelper.Configuration;
using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Enums;
using Hospital.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace Hospital.WebApi.Controllers;

[Route( "api/[controller]" )]
[Authorize(Roles = "Admin" )]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _usersService;
    private readonly IAuditService _auditService;

    public UserController(IUserService usersService, IAuditService auditService)
    {
        _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // GET: api/Users
    [HttpGet( "" )]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] UserQueryDto query)
    {
        var users = await _usersService.GetAllAsync().ConfigureAwait(false);
        if (users is null) return NotFound();
        return Ok( new PagedResult<UserDto>(
                    users.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize),
                    users.Count(),
                    query.Page,
                    query.PageSize
                 ));
    }

    // GET: api/Users/{id}
    [HttpGet( "{id:guid}" )]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await _usersService.GetByIdAsync(id).ConfigureAwait(false);
        if (user is null) return NotFound();
        return Ok(user);
    }

    // GET: api/Users/by-email?email=...
    [HttpGet( "by-email" )]
    public async Task<ActionResult<UserDto>> GetByEmail([FromQuery] string email)
    {
        var user = await _usersService.GetByEmailAsync(email).ConfigureAwait(false);
        if (user is null) return NotFound();
        return Ok(user);
    }

    // POST: api/Users/{id}/block
    [HttpPost( "{id:guid}/block" )]
    public async Task<IActionResult> Block(Guid id)
    {
        // Get the user to check role before blocking
        var user = await _usersService.GetByIdAsync(id).ConfigureAwait(false);
        if (user is null) return NotFound();

        if (user.Role == Role.Admin)
        {
            // You can't block admins
            return BadRequest( "Blocking an admin is not allowed." );
        }

        try
        {
            await _usersService.BlockAsync(id).ConfigureAwait(false);

            var actorId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(actorId, AuditAct.User, $"Blocked user. TargetUserId: {id} ByUserId: {actorId}" ).ConfigureAwait(false);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            var actorId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(actorId, AuditAct.User, $"Block failed. TargetUserId: {id} ByUserId: {actorId} Message: {ex.Message}" ).ConfigureAwait(false);
            throw;
        }
    }

    // POST: api/Users/{id}/unblock
    [HttpPost( "{id:guid}/unblock" )]
    public async Task<IActionResult> Unblock(Guid id)
    {
        // Get the user to check role before unblocking
        var user = await _usersService.GetByIdAsync(id).ConfigureAwait(false);
        if (user is null) return NotFound();

        if (user.Role == Role.Admin)
        {
            // You can't unblock admins
            return BadRequest( "Unblocking an admin is not allowed." );
        }

        try
        {
            await _usersService.UnblockAsync(id).ConfigureAwait(false);

            var actorId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(actorId, AuditAct.User, $"Unblocked user. TargetUserId: {id} ByUserId: {actorId}" ).ConfigureAwait(false);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            var actorId = HttpContext.GetCurrentUserId();
            await SafeLogAsync(actorId, AuditAct.User, $"Unblock failed. TargetUserId: {id} ByUserId: {actorId} Message: {ex.Message}" ).ConfigureAwait(false);
            throw;
        }
    }

    // GET: api/Users/export
    [HttpGet( "export" )]
    public async Task<IActionResult> Export()
    {
        var users = (await _usersService.GetAllAsync().ConfigureAwait(false)).ToList();

        // Using CsvHelper for correct CSV Serialization
        using var sw = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };

        using (var csv = new CsvWriter(sw, config))
        {
            // Specify format for DateTime
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "o" };

            csv.WriteHeader<UserDto>();
            csv.NextRecord();

            await csv.WriteRecordsAsync(users).ConfigureAwait(false);
        }

        var csvString = sw.ToString();
        var csvBytes = Encoding.UTF8.GetBytes(csvString);
        var fileName = $"users_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        var actor = HttpContext.GetCurrentUserId();
        await SafeLogAsync(actor, AuditAct.User, $"Exported users CSV. Count: {users.Count}" ).ConfigureAwait(false);

        return File(csvBytes, "text/csv; charset=utf-8", fileName);
    }

    // POST: api/Users/import
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

            IEnumerable<UserDto> records;
            try
            {
                records = csv.GetRecords<UserDto>().ToList();
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
                    UserDto? existing = null;
                    if (dto.Id != Guid.Empty)
                    {
                        existing = await _usersService.GetByIdAsync(dto.Id).ConfigureAwait(false);
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
                        errors.Add( $"Row {processed}: user not found (Id: '{dto.Id}', Email: '{dto.Email ?? ""}')." );
                        continue;
                    }

                    await _usersService.UpdateAsync(dto).ConfigureAwait(false);
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
            await SafeLogAsync(actorId, AuditAct.User, $"Import failed. Error: {ex.Message}" ).ConfigureAwait(false);
            throw;
        }

        await SafeLogAsync(actorId, AuditAct.User, $"Imported users CSV. Processed: {processed} Updated: {updated} Skipped: {skipped}" ).ConfigureAwait(false);

        return Ok( new ImportResultDto(processed, updated, skipped, errors) );
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