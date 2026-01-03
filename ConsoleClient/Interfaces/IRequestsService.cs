using Hospital.Application.DTOs;

namespace Hospital.ConsoleClient.Interfaces;

internal interface IRequestsService
{
    // Auth
    Task<AuthResultDto> RegisterAsync(UserRegisterDto dto);
    Task<AuthResultDto> LoginAsync(UserLoginDto dto);

    // Users
    Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task BlockUserAsync(Guid id);
    Task UnblockUserAsync(Guid id);
    Task<(byte[] Content, string FileName)> ExportUsersAsync(UserQueryDto? query = null);
    Task<ImportResultDto> ImportUsersAsync(byte[] csvContent);

    // Patients
    Task<PatientDto?> GetPatientByIdAsync(Guid id);
    Task<PagedResult<PatientDto>> SearchPatientsAsync(PatientQueryDto query);
    Task<(byte[] Content, string FileName)> ExportPatientsAsync(PatientQueryDto? query = null);
    Task<ImportResultDto> ImportPatientsAsync(byte[] csvContent);

    // Doctors
    Task<DoctorDto?> GetDoctorByIdAsync(Guid id);
    Task<PagedResult<DoctorDto>> SearchDoctorsAsync(DoctorQueryDto query);
    Task<(byte[] Content, string FileName)> ExportDoctorsAsync(DoctorQueryDto? query = null);
    Task<ImportResultDto> ImportDoctorsAsync(byte[] csvContent);
    Task<AuthResultDto> CreateDoctorAsync(DoctorRegisterDto dto);

    // Appointments
    Task<AppointmentBookResultDto> BookAppointmentAsync(BookAppointmentRequest dto);
    Task<PagedResult<AppointmentDto>> GetAppointmentsForPatientAsync(AppointmentQueryDto? query = null);
    Task<PagedResult<AppointmentDto>> GetAppointmentsForDoctorAsync(AppointmentQueryDto? query = null);
    Task<(byte[] Content, string FileName)> ExportAppointmentsAsync(AppointmentQueryDto? query = null);
    Task<ImportResultDto> ImportAppointmentsAsync(byte[] csvContent);
    Task CloseAppointmentAsync(Guid id, CloseAppointmentRequest dto);

    // Report
    Task<ReportResultDto> GenerateReportAsync(ReportRequestDto request);
    Task<(byte[] Content, string FileName)> ExportReportAsync(ReportRequestDto request);

    // Audit
    Task<PagedResult<AuditLogDto>> SearchAuditAsync(AuditLogQueryDto query);
    Task<(byte[] Content, string FileName)> ExportAuditAsync(AuditLogQueryDto? query = null);
}