using Microsoft.EntityFrameworkCore;
using Hospital.Domain.Entities;

namespace Hospital.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for working with SQLite. Contains DbSets for all entities, key configuration, indexes, etc.
/// </summary>
public class HospitalDbContext : DbContext
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Patient> Patients { get; set; } = null!;
    public DbSet<Doctor> Doctors { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable( "Users" );
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).IsRequired().HasMaxLength(256);
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.Role).IsRequired().HasConversion<int>();
            b.Property(x => x.IsBlocked).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
        });

        // Patients
        modelBuilder.Entity<Patient>(b =>
        {
            b.ToTable( "Patients" );
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.FirstName).IsRequired().HasMaxLength(128);
            b.Property(x => x.LastName).IsRequired().HasMaxLength(128);
            b.Property(x => x.Email).IsRequired().HasMaxLength(128);
            b.Property(x => x.Phone).IsRequired().HasMaxLength(10);
            b.Property(x => x.BirthDate).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Doctors
        modelBuilder.Entity<Doctor>(b =>
        {
            b.ToTable( "Doctors" );
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.FirstName).IsRequired().HasMaxLength(128);
            b.Property(x => x.LastName).IsRequired().HasMaxLength(128);
            b.Property(x => x.Specialty).IsRequired().HasConversion<int>();
            b.Property(x => x.Email).IsRequired().HasMaxLength(256);
            b.Property(x => x.Phone).IsRequired().HasMaxLength(10);
            b.Property(x => x.BirthDate).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Appointments
        modelBuilder.Entity<Appointment>(b =>
        {
            b.ToTable( "Appointments" );
            b.HasKey(x => x.Id);
            b.Property(x => x.PatientId).IsRequired();
            b.Property(x => x.DoctorId).IsRequired();
            b.Property(x => x.AppointmentTime).IsRequired();
            b.Property(x => x.Status).IsRequired().HasConversion<int>();
            b.Property(x => x.Notes).HasMaxLength(4096);
            b.Property(x => x.DoctorNotes).HasMaxLength(4096);
            b.Property(x => x.CreatedAt).IsRequired();
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.DoctorId);
            b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Doctor>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLogs
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable( "AuditLogs" );
            b.HasKey(x => x.Id);
            b.Property(x => x.Timestamp).IsRequired();
            b.Property(x => x.UserId).IsRequired(false);
            b.Property(x => x.Action).IsRequired().HasConversion<int>();
            b.Property(x => x.Details).IsRequired().HasMaxLength(4096);
            b.HasIndex(x => x.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }

    // Default configuration for SQLite if no options are provided
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite( "Data Source=hospital.db" );
        }
    }
}