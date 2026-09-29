using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.PatientNumber)
            .IsUnique();

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.IdNumber)
            .IsUnique();

        modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.EmployeeNumber)
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new
            {
                a.DoctorId,
                a.AppointmentDateTime
            })
            .IsUnique()
            .HasDatabaseName(
                "UX_Appointments_DoctorId_AppointmentDateTime_Active")
            .HasFilter(
                "[Status] IN ('Scheduled', 'Confirmed')");

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new
            {
                a.PatientId,
                a.AppointmentDateTime
            });

        modelBuilder.Entity<Consultation>()
            .HasIndex(c => c.AppointmentId)
            .IsUnique();

        modelBuilder.Entity<Prescription>()
            .HasIndex(p => p.ConsultationId);

        modelBuilder.Entity<MedicalRecord>()
            .HasIndex(m => new
            {
                m.PatientId,
                m.RecordedAtUtc
            });

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Consultation>()
            .HasOne(c => c.Appointment)
            .WithOne()
            .HasForeignKey<Consultation>(c => c.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Consultation>()
            .HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Consultation>()
            .HasOne(c => c.Doctor)
            .WithMany()
            .HasForeignKey(c => c.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Consultation)
            .WithMany(c => c.Prescriptions)
            .HasForeignKey(p => p.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Doctor)
            .WithMany()
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Patient)
            .WithMany()
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Doctor)
            .WithMany()
            .HasForeignKey(m => m.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}