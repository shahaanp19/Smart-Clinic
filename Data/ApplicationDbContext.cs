using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
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

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Role)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.Property(p => p.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.PatientNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.Email)
                .HasMaxLength(255);

            entity.Property(p => p.IdNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.Gender)
                .HasMaxLength(20);

            entity.Property(p => p.Address)
                .HasMaxLength(500);

            entity.HasIndex(p => p.PatientNumber)
                .IsUnique();

            entity.HasIndex(p => p.IdNumber)
                .IsUnique();
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.Property(d => d.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(d => d.EmployeeNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(d => d.Specialisation)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(d => d.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(d => d.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(d => d.Qualifications)
                .HasMaxLength(1000);

            entity.HasIndex(d => d.EmployeeNumber)
                .IsUnique();

            entity.HasIndex(d => d.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.Property(a => a.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(a => a.Reason)
                .HasMaxLength(500);

            entity.Property(a => a.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(a => new
            {
                a.DoctorId,
                a.AppointmentDateTime
            })
            .IsUnique()
            .HasDatabaseName(
                "UX_Appointments_DoctorId_AppointmentDateTime_Active")
            .HasFilter(
                "[Status] IN ('Scheduled', 'Confirmed')");

            entity.HasIndex(a => new
            {
                a.PatientId,
                a.AppointmentDateTime
            });

            entity.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.Property(c => c.Symptoms)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(c => c.Diagnosis)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(c => c.TreatmentPlan)
                .HasMaxLength(2000);

            entity.Property(c => c.ClinicalNotes)
                .HasMaxLength(2000);

            entity.HasIndex(c => c.AppointmentId)
                .IsUnique();

            entity.HasOne(c => c.Appointment)
                .WithOne()
                .HasForeignKey<Consultation>(
                    c => c.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Patient)
                .WithMany()
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Doctor)
                .WithMany()
                .HasForeignKey(c => c.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.Property(p => p.MedicationName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Dosage)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Frequency)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Duration)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Instructions)
                .HasMaxLength(1000);

            entity.HasIndex(p => p.ConsultationId);

            entity.HasOne(p => p.Consultation)
                .WithMany(c => c.Prescriptions)
                .HasForeignKey(p => p.ConsultationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.Property(m => m.RecordType)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(m => m.Description)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(m => m.ClinicalNotes)
                .HasMaxLength(2000);

            entity.HasIndex(m => new
            {
                m.PatientId,
                m.RecordedAtUtc
            });

            entity.HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Doctor)
                .WithMany()
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}