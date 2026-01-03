using Riok.Mapperly.Abstractions;
using Hospital.Application.DTOs;
using Hospital.Domain.Entities;

namespace Hospital.Application.Mappings
{
    [Mapper(AllowNullPropertyAssignment = false)]
    public partial class AppMapper
    {
        // User
        [MapperIgnoreSource(nameof(User.PasswordHash))]
        public partial UserDto UserToUserDto(User user);

        [MapperIgnoreSource(nameof(User.PasswordHash))]
        public partial IEnumerable<UserDto> UsersToUserDtos(IEnumerable<User> users);

        [MapperIgnoreTarget(nameof(User.Id))]
        [MapperIgnoreTarget(nameof(User.IsBlocked))]
        [MapperIgnoreTarget(nameof(User.CreatedAt))]
        public partial User CreateUserDtoToUser(CreateUserDto dto);

        // Patient
        public partial PatientDto PatientToPatientDto(Patient patient);

        [MapperIgnoreTarget(nameof(Patient.Id))]
        [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
        //[MapperIgnoreTarget(nameof(Patient.UserId))]
        public partial Patient CreatePatientDtoToPatient(CreatePatientDto dto);
       
        public partial IEnumerable<PatientDto> PatientsToPatientDtos(IEnumerable<Patient> patients);

        // Doctor
        public partial DoctorDto DoctorToDoctorDto(Doctor doctor);

        [MapperIgnoreTarget(nameof(Doctor.Id))]
        [MapperIgnoreTarget(nameof(Doctor.CreatedAt))]
        //[MapperIgnoreTarget(nameof(Doctor.UserId))]
        public partial Doctor CreateDoctorDtoToDoctor(CreateDoctorDto dto);

        public partial IEnumerable<DoctorDto> DoctorsToDoctorDtos(IEnumerable<Doctor> doctors);

        [MapperIgnoreSource(nameof(DoctorRegisterDto.Specialty))]
        public partial UserRegisterDto DoctorRegisterDtoToUserRegisterDto(DoctorRegisterDto dto);

        [MapperIgnoreTarget(nameof(DoctorRegisterDto.Specialty))]
        public partial DoctorRegisterDto UserRegisterDtoToDoctorRegisterDto(UserRegisterDto dto);

        // Appointment
        public partial AppointmentDto AppointmentToAppointmentDto(Appointment appointment);

        [MapperIgnoreTarget(nameof(Appointment.Id))]
        [MapperIgnoreTarget(nameof(Appointment.CreatedAt))]
        public partial Appointment CreateAppointmentDtoToAppointment(CreateAppointmentDto dto);

        public partial IEnumerable<AppointmentDto> AppointmentsToAppointmentDtos(IEnumerable<Appointment> appointments);

        // AuditLog
        public partial AuditLogDto AuditLogToAuditLogDto(AuditLog auditLog);
    }
}