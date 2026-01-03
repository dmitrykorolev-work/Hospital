using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Hospital.Application.Mappings;

namespace Hospital.Application.Services;

public sealed class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly AppMapper _mapper;

    public DoctorService(IDoctorRepository doctorRepository, AppMapper mapper)
    {
        _doctorRepository = doctorRepository ?? throw new ArgumentNullException(nameof(doctorRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<DoctorDto> CreateAsync(CreateDoctorDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.UserId == Guid.Empty) throw new ArgumentException( "UserId is required.", nameof(dto.UserId));
        if (string.IsNullOrWhiteSpace(dto.FirstName)) throw new ArgumentException( "FirstName is required.", nameof(dto.FirstName));
        if (string.IsNullOrWhiteSpace(dto.LastName)) throw new ArgumentException( "LastName is required.", nameof(dto.LastName));

        var existingByUser = await _doctorRepository.GetByUserIdAsync(dto.UserId).ConfigureAwait(false);
        if (existingByUser is not null)
        {
            throw new InvalidOperationException( "Doctor for given user already exists." );
        }

        var doctor = _mapper.CreateDoctorDtoToDoctor(dto);

        // It's more likely to break due to a random cosmic ray than because of a repeated 128-bit key, but I don't want to rely on chance.
        do doctor.Id = Guid.NewGuid();
        while (await _doctorRepository.GetByIdAsync(doctor.Id).ConfigureAwait(false) is not null);

        doctor.CreatedAt = DateTime.UtcNow;

        await _doctorRepository.AddAsync(doctor).ConfigureAwait(false);

        return _mapper.DoctorToDoctorDto(doctor);
    }

    public async Task<DoctorDto?> GetByIdAsync(Guid id)
    {
        var doctor = await _doctorRepository.GetByIdAsync(id).ConfigureAwait(false);
        return doctor is null ? null : _mapper.DoctorToDoctorDto(doctor);
    }

    public async Task<DoctorDto?> GetByUserIdAsync(Guid userId)
    {
        var doctor = await _doctorRepository.GetByUserIdAsync(userId).ConfigureAwait(false);
        return doctor is null ? null : _mapper.DoctorToDoctorDto(doctor);
    }

    public async Task<PagedResult<DoctorDto>> SearchAsync(DoctorQueryDto query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        if (query.BirthDateFrom.HasValue && query.BirthDateTo.HasValue &&
            query.BirthDateFrom.Value.Date > query.BirthDateTo.Value.Date)
        {
            throw new ArgumentException( "BirthDateFrom cannot be greater than BirthDateTo." );
        }

        var doctors = (await _doctorRepository.SearchAsync(query).ConfigureAwait(false)).AsQueryable();

        var sortBy = (query.SortBy ?? "LastName" ).Trim();
        var sortDir = (query.SortDir ?? "asc" ).Trim().ToLowerInvariant();

        doctors = (sortBy, sortDir) switch
        {
            ( "FirstName", "asc" ) => doctors.OrderBy(d => d.FirstName),
            ( "FirstName", "desc" ) => doctors.OrderByDescending(d => d.FirstName),
            ( "BirthDate", "asc" ) => doctors.OrderBy(d => d.BirthDate),
            ( "BirthDate", "desc" ) => doctors.OrderByDescending(d => d.BirthDate),
            ( "LastName", "desc" ) => doctors.OrderByDescending(d => d.LastName).ThenBy(d => d.FirstName),
            _ => doctors.OrderBy(d => d.LastName).ThenBy(d => d.FirstName),
        };

        var total = doctors.Count();
        var items = doctors
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.DoctorsToDoctorDtos(items);

        return new PagedResult<DoctorDto>(dtos, total, page, pageSize);
    }

    public async Task UpdateAsync(DoctorDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.Id == Guid.Empty) throw new ArgumentException( "Id is required.", nameof(dto.Id));
        if (dto.UserId == Guid.Empty) throw new ArgumentException( "UserId is required.", nameof(dto.UserId));
        if (string.IsNullOrWhiteSpace(dto.FirstName)) throw new ArgumentException( "FirstName is required.", nameof(dto.FirstName));
        if (string.IsNullOrWhiteSpace(dto.LastName)) throw new ArgumentException( "LastName is required.", nameof(dto.LastName));

        var doctor = await _doctorRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (doctor is null) throw new KeyNotFoundException( "Doctor not found." );

        // Check that another doctor is not using the same UserId
        var byUser = await _doctorRepository.GetByUserIdAsync(dto.UserId).ConfigureAwait(false);
        if (byUser is not null && byUser.Id != doctor.Id)
        {
            throw new InvalidOperationException( "Another doctor with the same UserId already exists." );
        }

        doctor.UserId = dto.UserId;
        doctor.FirstName = dto.FirstName;
        doctor.LastName = dto.LastName;
        doctor.BirthDate = dto.BirthDate;
        doctor.Phone = dto.Phone;
        doctor.Email = dto.Email;
        doctor.Specialty = dto.Specialty;
        // CreatedAt and Id are not changed

        await _doctorRepository.UpdateAsync(doctor).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id)
    {
        var doctor = await _doctorRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (doctor is null) throw new KeyNotFoundException( "Doctor not found." );

        await _doctorRepository.DeleteAsync(id).ConfigureAwait(false);
    }
}