IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Doctors] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [EmployeeNumber] nvarchar(50) NOT NULL,
    [Specialisation] nvarchar(100) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Qualifications] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Patients] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [PatientNumber] nvarchar(20) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Email] nvarchar(255) NULL,
    [IdNumber] nvarchar(20) NOT NULL,
    [DateOfBirth] datetime2 NOT NULL,
    [Gender] nvarchar(20) NULL,
    [Address] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(100) NOT NULL,
    [Role] nvarchar(50) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Appointments] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [AppointmentDateTime] datetime2 NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [MedicalRecords] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [RecordType] nvarchar(2000) NOT NULL,
    [Description] nvarchar(4000) NOT NULL,
    [ClinicalNotes] nvarchar(2000) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_MedicalRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MedicalRecords_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MedicalRecords_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Consultations] (
    [Id] int NOT NULL IDENTITY,
    [AppointmentId] int NOT NULL,
    [PatientId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [Symptoms] nvarchar(2000) NOT NULL,
    [Diagnosis] nvarchar(2000) NOT NULL,
    [TreatmentPlan] nvarchar(2000) NULL,
    [ClinicalNotes] nvarchar(2000) NULL,
    [ConsultationDateUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Consultations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Consultations_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Consultations_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Consultations_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Prescriptions] (
    [Id] int NOT NULL IDENTITY,
    [ConsultationId] int NOT NULL,
    [PatientId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [MedicationName] nvarchar(200) NOT NULL,
    [Dosage] nvarchar(100) NOT NULL,
    [Frequency] nvarchar(100) NOT NULL,
    [Duration] nvarchar(100) NOT NULL,
    [Instructions] nvarchar(1000) NULL,
    [PrescribedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_Prescriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Prescriptions_Consultations_ConsultationId] FOREIGN KEY ([ConsultationId]) REFERENCES [Consultations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Prescriptions_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Prescriptions_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_Appointments_PatientId_AppointmentDateTime] ON [Appointments] ([PatientId], [AppointmentDateTime]);
GO

CREATE UNIQUE INDEX [UX_Appointments_DoctorId_AppointmentDateTime_Active] ON [Appointments] ([DoctorId], [AppointmentDateTime]) WHERE [Status] IN ('Scheduled', 'Confirmed');
GO

CREATE UNIQUE INDEX [IX_Consultations_AppointmentId] ON [Consultations] ([AppointmentId]);
GO

CREATE INDEX [IX_Consultations_DoctorId] ON [Consultations] ([DoctorId]);
GO

CREATE INDEX [IX_Consultations_PatientId] ON [Consultations] ([PatientId]);
GO

CREATE UNIQUE INDEX [IX_Doctors_EmployeeNumber] ON [Doctors] ([EmployeeNumber]);
GO

CREATE INDEX [IX_MedicalRecords_DoctorId] ON [MedicalRecords] ([DoctorId]);
GO

CREATE INDEX [IX_MedicalRecords_PatientId_RecordedAtUtc] ON [MedicalRecords] ([PatientId], [RecordedAtUtc]);
GO

CREATE UNIQUE INDEX [IX_Patients_IdNumber] ON [Patients] ([IdNumber]);
GO

CREATE UNIQUE INDEX [IX_Patients_PatientNumber] ON [Patients] ([PatientNumber]);
GO

CREATE INDEX [IX_Prescriptions_ConsultationId] ON [Prescriptions] ([ConsultationId]);
GO

CREATE INDEX [IX_Prescriptions_DoctorId] ON [Prescriptions] ([DoctorId]);
GO

CREATE INDEX [IX_Prescriptions_PatientId] ON [Prescriptions] ([PatientId]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260929022823_InitialCreate', N'8.0.20');
GO

COMMIT;
GO

