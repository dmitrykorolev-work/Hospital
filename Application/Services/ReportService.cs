using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Domain.Entities;

namespace Hospital.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IAppointmentRepository _appointmentRepository;

    public ReportService(IPatientRepository patientRepository, IAppointmentRepository appointmentRepository)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
    }

    public async Task<ReportResultDto> GenerateReportAsync(ReportRequestDto request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var now = DateTime.UtcNow;
        int totalPatients;
        int totalAppointments;
        double averageAge;

        var hasAnyFilter = request.From.HasValue || request.To.HasValue || request.DoctorId.HasValue || request.PatientId.HasValue;

        if (!hasAnyFilter)
        {
            // No filters - whole system report
            var allPatients = (await _patientRepository.GetAllAsync().ConfigureAwait(false)).ToList();
            totalPatients = allPatients.Count;
            averageAge = totalPatients > 0
                ? allPatients.Select(p => CalculateExactAgeYears(p.BirthDate, now)).Average()
                : 0.0;

            var allAppointments = (await _appointmentRepository.GetAllAsync().ConfigureAwait(false)).ToList();
            totalAppointments = allAppointments.Count;
        }
        else
        {
            // Build appointment query from request filters
            var appointmentQuery = new AppointmentQueryDto(
                Page: 1,
                PageSize: int.MaxValue,
                DoctorId: request.DoctorId,
                PatientId: request.PatientId,
                From: request.From,
                To: request.To
            );

            var appointments = (await _appointmentRepository.SearchAsync(appointmentQuery).ConfigureAwait(false)).ToList();
            totalAppointments = appointments.Count;

            if (request.PatientId.HasValue)
            {
                // Specific patient requested
                var patient = await _patientRepository.GetByIdAsync(request.PatientId.Value).ConfigureAwait(false);
                if (patient is null)
                {
                    totalPatients = 0;
                    averageAge = 0.0;
                }
                else
                {
                    totalPatients = 1;
                    averageAge = CalculateExactAgeYears(patient.BirthDate, now);
                }
            }
            else
            {
                // Get patient set from appointments (unique patient ids)
                var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();

                if (patientIds.Count == 0)
                {
                    totalPatients = 0;
                    averageAge = 0.0;
                }
                else
                {
                    var patients = new List<Patient>();
                    foreach (var id in patientIds)
                    {
                        var p = await _patientRepository.GetByIdAsync(id).ConfigureAwait(false);
                        if (p is not null) patients.Add(p);
                    }

                    totalPatients = patients.Count;
                    averageAge = totalPatients > 0
                        ? patients.Select(p => CalculateExactAgeYears(p.BirthDate, now)).Average()
                        : 0.0;
                }
            }
        }

        // Round average age to 2 decimal places for readability
        var roundedAverageAge = Math.Round(averageAge, 2);

        return new ReportResultDto(totalPatients, totalAppointments, roundedAverageAge, now);
    }

    private static double CalculateExactAgeYears(DateTime birthDate, DateTime now)
    {
        // Use average year length
        var days = (now - birthDate).TotalDays;
        return days / 365.2425;
    }
}