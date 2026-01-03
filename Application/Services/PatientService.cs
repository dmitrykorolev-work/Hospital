using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;

namespace Hospital.Application.Services;

public sealed class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly AppMapper _mapper;

    public PatientService(IPatientRepository patientRepository, AppMapper mapper)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.UserId == Guid.Empty) throw new ArgumentException( "UserId is required.", nameof(dto.UserId));
        if (string.IsNullOrWhiteSpace(dto.FirstName)) throw new ArgumentException( "FirstName is required.", nameof(dto.FirstName));
        if (string.IsNullOrWhiteSpace(dto.LastName)) throw new ArgumentException( "LastName is required.", nameof(dto.LastName));

        // Check that another patient is not using the same UserId
        var existingByUser = await _patientRepository.GetByUserIdAsync(dto.UserId).ConfigureAwait(false);
        if (existingByUser is not null)
        {
            throw new InvalidOperationException( "Patient for given user already exists." );
        }

        var patient = _mapper.CreatePatientDtoToPatient(dto);

        // It’s more likely to break due to the universe running out of entropy and reusing states than because of a repeated 128-bit key, but I don’t want to rely on chance.
        do patient.Id = Guid.NewGuid();
        while (await _patientRepository.GetByIdAsync(patient.Id).ConfigureAwait(false) is not null);

        patient.CreatedAt = DateTime.UtcNow;

        await _patientRepository.AddAsync(patient).ConfigureAwait(false);

        return _mapper.PatientToPatientDto(patient);
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id)
    {
        var patient = await _patientRepository.GetByIdAsync(id).ConfigureAwait(false);
        return patient is null ? null : _mapper.PatientToPatientDto(patient);
    }

    public async Task<PatientDto?> GetByUserIdAsync(Guid userId)
    {
        var patient = await _patientRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
        return patient is null ? null : _mapper.PatientToPatientDto(patient);
    }

    public async Task<PagedResult<PatientDto>> SearchAsync(PatientQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        if (query.BirthDateFrom.HasValue && query.BirthDateTo.HasValue &&
            query.BirthDateFrom.Value.Date > query.BirthDateTo.Value.Date)
        {
            throw new ArgumentException( "BirthDateFrom cannot be greater than BirthDateTo." );
        }

        // Repository returns a filtered set based on the passed PatientQueryDto
        var patients = (await _patientRepository.SearchAsync(query).ConfigureAwait(false)).AsQueryable();

        // Sorting: Support a limited set of fields, by default LastName asc, then FirstName.
        var sortBy = (query.SortBy ?? "LastName" ).Trim();
        var sortDir = (query.SortDir ?? "asc" ).Trim().ToLowerInvariant();

        patients = (sortBy, sortDir) switch
        {
            ( "FirstName", "asc" ) => patients.OrderBy(p => p.FirstName),
            ( "FirstName", "desc" ) => patients.OrderByDescending(p => p.FirstName),
            ( "BirthDate", "asc" ) => patients.OrderBy(p => p.BirthDate),
            ( "BirthDate", "desc" ) => patients.OrderByDescending(p => p.BirthDate),
            ( "LastName", "desc" ) => patients.OrderByDescending(p => p.LastName).ThenBy(p => p.FirstName),
            _ => patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
        };

        var total = patients.Count();
        var items = patients
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.PatientsToPatientDtos(items);

        return new PagedResult<PatientDto>(dtos, total, page, pageSize);
    }

    public async Task UpdateAsync(PatientDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.Id == Guid.Empty) throw new ArgumentException( "Id is required.", nameof(dto.Id));
        if (dto.UserId == Guid.Empty) throw new ArgumentException( "UserId is required.", nameof(dto.UserId));
        if (string.IsNullOrWhiteSpace(dto.FirstName)) throw new ArgumentException( "FirstName is required.", nameof(dto.FirstName));
        if (string.IsNullOrWhiteSpace(dto.LastName)) throw new ArgumentException( "LastName is required.", nameof(dto.LastName));

        var patient = await _patientRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (patient is null) throw new KeyNotFoundException( "Patient not found." );

        // Check that another patient is not using the same UserId
        var byUser = await _patientRepository.GetByUserIdAsync(dto.UserId).ConfigureAwait(false);
        if (byUser is not null && byUser.Id != patient.Id)
        {
            throw new InvalidOperationException( "Another patient with the same UserId already exists." );
        }

        patient.UserId = dto.UserId;
        patient.FirstName = dto.FirstName;
        patient.LastName = dto.LastName;
        patient.BirthDate = dto.BirthDate;
        patient.Phone = dto.Phone;
        patient.Email = dto.Email;
        // CreatedAt and Id are not changed

        await _patientRepository.UpdateAsync(patient).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id)
    {
        var patient = await _patientRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (patient is null) throw new KeyNotFoundException( "Patient not found." );

        await _patientRepository.DeleteAsync(id).ConfigureAwait(false);
    }
}