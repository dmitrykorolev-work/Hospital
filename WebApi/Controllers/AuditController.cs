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
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // GET: api/Audit
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Search([FromQuery] AuditLogQueryDto? query)
    {
        query ??= new AuditLogQueryDto();

        var userId = HttpContext.GetCurrentUserId();

        try
        {
            var result = await _auditService.SearchAsync(query).ConfigureAwait(false);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            throw;
        }
    }

    // GET: api/Audit/export
    [HttpGet( "export" )]
    public async Task<IActionResult> Export([FromQuery] AuditLogQueryDto? query)
    {
        query ??= new AuditLogQueryDto();

        // For export, return all matching records: Page = 1, PageSize = int.MaxValue
        var q = query with { Page = 1, PageSize = int.MaxValue };

        var result = await _auditService.SearchAsync(q).ConfigureAwait(false);
        var auditList = result.Items.ToList();

        // Using CsvHelper for correct CSV Serialization
        using var sw = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };

        using (var csv = new CsvWriter(sw, config))
        {
            // Specify format for DateTime
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = ["o"];

            csv.WriteHeader<AuditLogDto>();
            csv.NextRecord();

            await csv.WriteRecordsAsync(auditList).ConfigureAwait(false);
        }

        var csvString = sw.ToString();
        var csvBytes = Encoding.UTF8.GetBytes(csvString);
        var fileName = $"audit_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        await SafeLogAsync(HttpContext.GetCurrentUserId(), AuditAct.User, $"Exported audit CSV. Count:{result.TotalCount}" ).ConfigureAwait(false);

        return File(csvBytes, "text/csv; charset=utf-8", fileName);
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