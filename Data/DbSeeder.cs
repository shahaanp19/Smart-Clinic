using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Data;

public static class DbSeeder
{
    private const string AdminEmail = "admin@smartclinic.local";
    private const string DoctorEmail = "doctor@smartclinic.local";
    private const string ReceptionistEmail = "reception@smartclinic.local";
    private const string PatientEmail = "patient@smartclinic.local";

    private const string AdminPassword = "Admin123!";
    private const string DoctorPassword = "Doctor123!";
    private const string ReceptionistPassword = "Reception123!";
    private const string PatientPassword = "Patient123!";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        var passwordHasher = new PasswordHasher<User>();

        /*
         * Create or repair the four application login accounts.
         */
        await SeedOrRepairUserAsync(
            context,
            passwordHasher,
            AdminEmail,
            "System Administrator",
            "Administrator",
            AdminPassword);

        await SeedOrRepairUserAsync(
            context,
            passwordHasher,
            DoctorEmail,
            "Demo Doctor",
            "Doctor",
            DoctorPassword);

        await SeedOrRepairUserAsync(
            context,
            passwordHasher,
            ReceptionistEmail,
            "Demo Receptionist",
            "Receptionist",
            ReceptionistPassword);

        await SeedOrRepairUserAsync(
            context,
            passwordHasher,
            PatientEmail,
            "Demo Patient",
            "Patient",
            PatientPassword);

        /*
         * The Doctor and Patient portals require a corresponding
         * Doctor or Patient record after authentication.
         */
        await SeedOrRepairDoctorAsync(context);

        await SeedOrRepairPatientAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedOrRepairUserAsync(
        ApplicationDbContext context,
        PasswordHasher<User> passwordHasher,
        string email,
        string fullName,
        string role,
        string password)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            user = new User
            {
                FullName = fullName,
                Email = email,
                Role = role,
                PhoneNumber = null,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    password);

            await context.Users.AddAsync(user);

            return;
        }

        user.FullName = fullName;
        user.Email = email;
        user.Role = role;
        user.IsActive = true;
        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                password);
        user.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static async Task SeedOrRepairDoctorAsync(
        ApplicationDbContext context)
    {
        var doctor = await context.Doctors
            .FirstOrDefaultAsync(d =>
                d.Email.ToLower() == DoctorEmail);

        if (doctor is null)
        {
            doctor = new Doctor
            {
                FullName = "Demo Doctor",
                EmployeeNumber = "DOC0001",
                Specialisation = "General Practitioner",
                Email = DoctorEmail,
                PhoneNumber = "0120000001",
                Qualifications = "MBChB",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await context.Doctors.AddAsync(doctor);

            return;
        }

        doctor.FullName = "Demo Doctor";
        doctor.EmployeeNumber = "DOC0001";
        doctor.Specialisation = "General Practitioner";
        doctor.Email = DoctorEmail;
        doctor.PhoneNumber = "0120000001";
        doctor.Qualifications = "MBChB";
        doctor.IsActive = true;
        doctor.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static async Task SeedOrRepairPatientAsync(
        ApplicationDbContext context)
    {
        var patient = await context.Patients
            .FirstOrDefaultAsync(p =>
                p.Email != null &&
                p.Email.ToLower() == PatientEmail);

        if (patient is null)
        {
            patient = new Patient
            {
                FullName = "Demo Patient",
                PatientNumber = "PAT0001",
                PhoneNumber = "0120000002",
                Email = PatientEmail,
                IdNumber = "9001015009087",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Other",
                Address = "Doringkloof, Pretoria",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await context.Patients.AddAsync(patient);

            return;
        }

        patient.FullName = "Demo Patient";
        patient.PatientNumber = "PAT0001";
        patient.PhoneNumber = "0120000002";
        patient.Email = PatientEmail;
        patient.IdNumber = "9001015009087";
        patient.DateOfBirth = new DateTime(1990, 1, 1);
        patient.Gender = "Other";
        patient.Address = "Doringkloof, Pretoria";
        patient.IsActive = true;
        patient.UpdatedAtUtc = DateTime.UtcNow;
    }
}