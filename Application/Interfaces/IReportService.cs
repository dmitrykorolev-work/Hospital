using Hospital.Application.DTOs;

namespace Hospital.Application.Interfaces;

public interface IReportService
{
    Task<ReportResultDto> GenerateReportAsync(ReportRequestDto request);
}
