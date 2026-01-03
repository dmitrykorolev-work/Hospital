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
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IAuditService _auditService;

    public ReportController(IReportService reportService, IAuditService auditService)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // GET: api/Report/generate
    [HttpGet( "generate" )]
    public async Task<ActionResult<ReportResultDto>> Generate([FromQuery] ReportRequestDto request)
    {
        if (request is null) return BadRequest();

        var result = await _reportService.GenerateReportAsync(request).ConfigureAwait(false);
        var actor = HttpContext.GetCurrentUserId();
        await SafeLogAsync(actor, AuditAct.Patient, $"Generated report. From: {request.From:o} To: {request.To:o} DoctorId: {request.DoctorId} PatientId: {request.PatientId}" ).ConfigureAwait(false);

        return Ok(result);
    }

    // GET: api/Report/export
    [HttpGet( "export" )]
    public async Task<IActionResult> Export([FromQuery] ReportRequestDto request)
    {
        var result = await _reportService.GenerateReportAsync(request).ConfigureAwait(false);

        // Using CsvHelper for correct CSV Serialization
        using var sw = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };

        using (var csv = new CsvWriter(sw, config))
        {
            // Specify format for DateTime
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "o" };

            csv.WriteHeader<ReportResultDto>();
            csv.NextRecord();

            csv.WriteRecord(result);
            csv.NextRecord();
        }

        var csvString = sw.ToString();
        var csvBytes = Encoding.UTF8.GetBytes(csvString);
        var fileName = $"report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        var actor = HttpContext.GetCurrentUserId();
        await SafeLogAsync(actor, AuditAct.Patient, $"Exported report CSV. DoctorId: {request.DoctorId} PatientId: {request.PatientId} From: {request.From:o} To: {request.To:o}" ).ConfigureAwait(false);

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