using Hospital.ConsoleClient.Interfaces;
using Hospital.Application.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace Hospital.ConsoleClient;


// TODO: Separate services?
internal class RequestsService : IRequestsService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _accessToken;

    public RequestsService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    // Auth
    public async Task<AuthResultDto> RegisterAsync(UserRegisterDto dto)
    {
        var result = await PostJsonAsync<UserRegisterDto, AuthResultDto>( "api/Auth/register", dto).ConfigureAwait(false);
        var token = result.Token.ToString();

        if (result is not null && result.Success && !string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", token);
        }

        return result;
    }

    public async Task<AuthResultDto> LoginAsync(UserLoginDto dto)
    {
        var result = await PostJsonAsync<UserLoginDto, AuthResultDto>( "api/Auth/login", dto).ConfigureAwait(false);

        var token = result.Token.ToString();

        if (result is not null && result.Success && !string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", token);
        }

        return result;
    }

    // Users
    public async Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query)
    {
        var url = $"api/User{BuildQueryString(query)}";
        return await GetJsonAsync<PagedResult<UserDto>>(url).ConfigureAwait(false);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        return await GetJsonAsync<UserDto?>( $"api/User/{id}" ).ConfigureAwait(false);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var url = $"api/User/by-email?email={Uri.EscapeDataString(email ?? "" )}";
        return await GetJsonAsync<UserDto?>(url).ConfigureAwait(false);
    }

    public async Task BlockUserAsync(Guid id)
    {
        await PostNoContentAsync( $"api/User/{id}/block" ).ConfigureAwait(false);
    }

    public async Task UnblockUserAsync(Guid id)
    {
        await PostNoContentAsync( $"api/User/{id}/unblock" ).ConfigureAwait(false);
    }

    public async Task<(byte[] Content, string FileName)> ExportUsersAsync(UserQueryDto? query = null)
    {
        var url = $"api/User/export{BuildQueryString(query)}";
        return await GetFileAsync(url).ConfigureAwait(false);
    }

    public async Task<ImportResultDto> ImportUsersAsync(byte[] csvContent)
    {
        return await PostFileImportAsync( "api/User/import", csvContent, "users.csv" ).ConfigureAwait(false);
    }

    // Patients
    public async Task<PatientDto?> GetPatientByIdAsync(Guid id)
    {
        return await GetJsonAsync<PatientDto?>( $"api/Patient/{id}" ).ConfigureAwait(false);
    }

    public async Task<PagedResult<PatientDto>> SearchPatientsAsync(PatientQueryDto query)
    {
        var url = $"api/Patient{BuildQueryString(query)}";
        return await GetJsonAsync<PagedResult<PatientDto>>(url).ConfigureAwait(false);
    }

    public async Task<(byte[] Content, string FileName)> ExportPatientsAsync(PatientQueryDto? query = null)
    {
        var url = $"api/Patient/export{BuildQueryString(query)}";
        return await GetFileAsync(url).ConfigureAwait(false);
    }

    public async Task<ImportResultDto> ImportPatientsAsync(byte[] csvContent)
    {
        return await PostFileImportAsync( "api/Patient/import", csvContent, "patients.csv" ).ConfigureAwait(false);
    }

    // Doctors
    public async Task<DoctorDto?> GetDoctorByIdAsync(Guid id)
    {
        return await GetJsonAsync<DoctorDto?>( $"api/Doctor/{id}" ).ConfigureAwait(false);
    }

    public async Task<PagedResult<DoctorDto>> SearchDoctorsAsync(DoctorQueryDto query)
    {
        var url = $"api/Doctor{BuildQueryString(query)}";
        return await GetJsonAsync<PagedResult<DoctorDto>>(url).ConfigureAwait(false);
    }

    public async Task<(byte[] Content, string FileName)> ExportDoctorsAsync(DoctorQueryDto? query = null)
    {
        var url = $"api/Doctor/export{BuildQueryString(query)}";
        return await GetFileAsync(url).ConfigureAwait(false);
    }

    public async Task<ImportResultDto> ImportDoctorsAsync(byte[] csvContent)
    {
        return await PostFileImportAsync( "api/Doctor/import", csvContent, "doctors.csv" ).ConfigureAwait(false);
    }

    public async Task<AuthResultDto> CreateDoctorAsync(DoctorRegisterDto dto)
    {
        return await PostJsonAsync<DoctorRegisterDto, AuthResultDto>( "api/Doctor", dto).ConfigureAwait(false);
    }

    // Appointments
    public async Task<AppointmentBookResultDto> BookAppointmentAsync(BookAppointmentRequest dto)
    {
        return await PostJsonAsync<BookAppointmentRequest, AppointmentBookResultDto>( "api/Appointment/book", dto).ConfigureAwait(false);
    }

    public async Task<PagedResult<AppointmentDto>> GetAppointmentsForPatientAsync(AppointmentQueryDto? query = null)
    {
        var url = $"api/Appointment/patient{BuildQueryString(query)}";
        return await GetJsonAsync<PagedResult<AppointmentDto>>(url).ConfigureAwait(false);
    }

    public async Task<PagedResult<AppointmentDto>> GetAppointmentsForDoctorAsync(AppointmentQueryDto? query = null)
    {
        var url = $"api/Appointment/doctor{BuildQueryString(query)}";
        return await GetJsonAsync<PagedResult<AppointmentDto>>(url).ConfigureAwait(false);
    }

    public async Task<(byte[] Content, string FileName)> ExportAppointmentsAsync(AppointmentQueryDto? query = null)
    {
        var url = $"api/Appointment/export{BuildQueryString(query)}";
        return await GetFileAsync(url).ConfigureAwait(false);
    }

    public async Task<ImportResultDto> ImportAppointmentsAsync(byte[] csvContent)
    {
        return await PostFileImportAsync( "api/Appointment/import", csvContent, "appointments.csv" ).ConfigureAwait(false);
    }

    public async Task CloseAppointmentAsync(Guid id, CloseAppointmentRequest dto)
    {
        var url = $"api/Appointment/{id}/close";
        await PostJsonNoResponseAsync(url, dto).ConfigureAwait(false);
    }

    // Report
    public async Task<ReportResultDto> GenerateReportAsync(ReportRequestDto request)
    {
        var url = $"api/Report/generate{BuildQueryString(request)}";
        return await GetJsonAsync<ReportResultDto>(url).ConfigureAwait(false);
    }

    public async Task<(byte[] Content, string FileName)> ExportReportAsync(ReportRequestDto request)
    {
        var url = $"api/Report/export{BuildQueryString(request)}";
        return await GetFileAsync(url).ConfigureAwait(false);
    }

    // Audit
    public async Task<PagedResult<AuditLogDto>> SearchAuditAsync(AuditLogQueryDto query)
    {
        var url = $"api/Audit{BuildQueryString(query)}";
        return await GetJsonAsync<PagedResult<AuditLogDto>>(url).ConfigureAwait(false);
    }

    public async Task<(byte[] Content, string FileName)> ExportAuditAsync(AuditLogQueryDto? query = null)
    {
        var url = $"api/Audit/export{BuildQueryString(query)}";
        return await GetFileAsync(url).ConfigureAwait(false);
    }

    // --- Helpers ---

    private async Task<T> GetJsonAsync<T>(string relativeUrl)
    {
        using var resp = await _http.GetAsync(relativeUrl).ConfigureAwait(false);

        var msg = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.NotFound) throw new KeyNotFoundException(msg);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(msg);
        }

        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<T>(_jsonOptions).ConfigureAwait(false);
        if (result is null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
        {
            throw new InvalidOperationException( "Response content is empty or cannot be deserialized." );
        }

        return result!;
    }

    private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string relativeUrl, TRequest dto)
    {
        using var content = JsonContent.Create(dto, options: _jsonOptions);
        using var resp = await _http.PostAsync(relativeUrl, content).ConfigureAwait(false);

        var msg = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        switch (resp.StatusCode)
        {
            case HttpStatusCode.NotFound:
                throw new KeyNotFoundException(msg);

            case HttpStatusCode.BadRequest:
                throw new ArgumentException(msg);

            case HttpStatusCode.Conflict:
                throw new InvalidOperationException(msg);
        }

        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<TResponse>(_jsonOptions).ConfigureAwait(false);
        if (result is null && typeof(TResponse).IsValueType && Nullable.GetUnderlyingType(typeof(TResponse)) == null)
        {
            throw new InvalidOperationException( "Response content is empty or cannot be deserialized." );
        }

        return result!;
    }

    private async Task PostJsonNoResponseAsync<TRequest>(string relativeUrl, TRequest dto)
    {
        using var content = JsonContent.Create(dto, options: _jsonOptions);
        using var resp = await _http.PostAsync(relativeUrl, content).ConfigureAwait(false);

        var msg = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.NotFound) throw new KeyNotFoundException(msg);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(msg);
        }

        resp.EnsureSuccessStatusCode();
    }

    private async Task PostNoContentAsync(string relativeUrl)
    {
        using var resp = await _http.PostAsync(relativeUrl, null).ConfigureAwait(false);

        var msg = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.NotFound) throw new KeyNotFoundException(msg);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(msg);
        }

        resp.EnsureSuccessStatusCode();
    }

    private async Task<ImportResultDto> PostFileImportAsync(string relativeUrl, byte[] csvContent, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(csvContent);
        bytes.Headers.ContentType = new MediaTypeHeaderValue( "text/csv" );
        content.Add(bytes, "file", fileName);

        using var resp = await _http.PostAsync(relativeUrl, content).ConfigureAwait(false);

        var msg = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.NotFound) throw new KeyNotFoundException(msg);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(msg);
        }

        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<ImportResultDto>(_jsonOptions).ConfigureAwait(false);
        return result ?? new ImportResultDto(0, 0, 0, new List<string>());
    }

    private async Task<(byte[] Content, string FileName)> GetFileAsync(string relativeUrl)
    {
        using var resp = await _http.GetAsync(relativeUrl).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        // Trying to get filename from header
        string fileName = "file.bin";
        if (resp.Content.Headers.ContentDisposition != null)
        {
            fileName = resp.Content.Headers.ContentDisposition.FileNameStar
                       ?? resp.Content.Headers.ContentDisposition.FileName
                       ?? fileName;
            fileName = fileName.Trim('"');
        }
        else
        {
            fileName = $"file_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        return (bytes, fileName);
    }

    // Build query string from object public properties (Actually some GPT code lol)
    private static string BuildQueryString(object? obj)
    {
        if (obj == null) return string.Empty;

        var props = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        var pairs = new List<string>();

        foreach (var p in props)
        {
            var value = p.GetValue(obj);
            if (value == null) continue;

            var name = Uri.EscapeDataString(ToCamelCase(p.Name));

            if (value is string s)
            {
                if ( string.IsNullOrWhiteSpace(s) ) continue;
                pairs.Add( $"{name}={ Uri.EscapeDataString(s) }" );
            }
            else if (value is DateTime dt)
            {
                pairs.Add( $"{name}={ Uri.EscapeDataString( dt.ToString( "o" ) ) }" );
            }
            else if (value is DateTimeOffset dto)
            {
                pairs.Add( $"{name}={ Uri.EscapeDataString( dto.ToString( "o" ) ) }" );
            }
            else if (value is bool b)
            {
                pairs.Add( $"{name}={ Uri.EscapeDataString( b.ToString().ToLowerInvariant() ) }" );
            }
            else if (IsNumericType(value))
            {
                pairs.Add( $"{name}={ Uri.EscapeDataString( Convert.ToString( value, System.Globalization.CultureInfo.InvariantCulture ) ?? "" ) }" );
            }
            else
            {
                // Enums and other types - just ToString
                var str = value.ToString();
                if ( string.IsNullOrEmpty(str) ) continue;
                pairs.Add( $"{name}={ Uri.EscapeDataString(str) }" );
            }
        }

        if ( !pairs.Any() ) return string.Empty;
        return "?" + string.Join( "&", pairs);
    }

    // Looks terrible, but IDK how to make it better.
    // Literally https://i.redd.it/secqqwjqz1e51.jpg
    private static bool IsNumericType(object value)
    {
        return value is sbyte || value is byte ||
               value is short || value is ushort ||
               value is int || value is uint ||
               value is long || value is ulong ||
               value is float || value is double || value is decimal;
    }

    // Convert PascalCase to camelCase (simple)
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0])) return name;
        if (name.Length == 1) return name.ToLowerInvariant();
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
