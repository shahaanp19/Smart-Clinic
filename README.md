# Smart Clinic Management System
---

# Group members

- Raghav Mahraj
- Shahaan Pillay
- Migyle Rajkoomar
- Rish

---  

## Project Overview

The **Smart Clinic Management System** is a web-based healthcare management solution developed for **Doringkloof Medical Centre**. The system provides a centralised platform for managing patients, doctors, reception operations, appointments, consultations, medical information, and administrative functions.

The application is developed using **ASP.NET Core MVC** and follows a layered architecture to provide a secure, maintainable, and scalable solution. It supports multiple user roles, role-based access control, database-driven operations, automated testing, CI/CD, and cloud deployment through Microsoft Azure.

---

## Project Objectives

The primary objectives of the Smart Clinic Management System is to:

- Digitise and streamline clinic management processes.
- Provide secure access to healthcare information based on user roles.
- Allow reception staff to manage patients and appointments efficiently.
- Allow doctors to access relevant patient information and record consultations.
- Provide patients with secure access to their appointments and healthcare information.
- Provide administrators with tools to manage users and system operations.
- Maintain reliable and structured healthcare data using a relational database.
- Implement authentication, authorization, input validation, and secure password management.
- Provide a responsive and user-friendly interface across desktop, tablet, and mobile devices.
- Implement automated testing to verify system functionality and reliability.
- Support continuous integration and continuous deployment through GitHub Actions.
- Deploy the application to Microsoft Azure for live access.

  ---

## Key Features

### Authentication and Security

- Secure user login and logout.
- Cookie-based authentication.
- Role-based authorization.
- Protected user portals and controller actions.
- Secure password hashing using ASP.NET Core Identity's `PasswordHasher`.
- Authentication-state validation.
- Anti-forgery protection.
- Return URL validation.
- Access-denied handling.

### Patient Management

- Patient profile management.
- Patient identification and contact information.
- Patient appointment access.
- Patient-specific healthcare information.
- Secure patient portal access.

### Doctor Management

- Doctor profiles and professional information.
- Doctor-specific dashboard.
- Appointment and consultation management.
- Access to relevant patient information.
- Consultation recording functionality.

### Reception Management

- Patient administration.
- Appointment booking and management.
- Receptionist-specific dashboard.
- Appointment scheduling between patients and doctors.

### Administrator Management

- Administrator dashboard.
- User management.
- User creation and account management.
- Role assignment.
- Account activation and deactivation.
- Administrative reporting functionality.

### Database Management

- Entity Framework Core integration.
- SQL Server relational database.
- Structured entity relationships.
- Database migrations.
- Data integrity constraints.
- Query indexes.
- Database seeding and repair functionality.

### Testing

- Automated unit and integration testing.
- Authentication and authorization testing.
- Controller testing.
- Service-layer testing.
- Database-related testing.
- Automated test execution through GitHub Actions.
- 226 automated tests successfully passing.

### Deployment and CI/CD

- GitHub source control.
- Feature, develop, and main branching workflow.
- Pull-request-based development.
- Protected main branch.
- GitHub Actions workflows.
- Automated build and testing.
- Automated deployment to Microsoft Azure.
- Live cloud-hosted application.
- Cloud database connectivity.

---

## User Roles and Permissions

| Role | Main Responsibilities |
|---|---|
| **Administrator** | Manage users, roles, accounts, administrative functions, and system-level operations. |
| **Doctor** | Access the doctor portal, view relevant appointments and patient information, and record consultations. |
| **Receptionist** | Manage patients, schedule appointments, and perform reception-related administrative functions. |
| **Patient** | Access personal information, appointments, and available healthcare-related information through the patient portal. |

Access to protected functionality is controlled using **ASP.NET Core authentication and role-based authorization policies**. Users are redirected to the appropriate role-specific dashboard after successful authentication.

---


## Technical Implementation

### 1. Technology Stack

The Smart Clinic Management System is developed using a modern Microsoft-based technology stack:

- **ASP.NET Core MVC** for the web application and server-side request handling.
- **C#** as the primary programming language.
- **Entity Framework Core** for object-relational mapping and database access.
- **SQL Server** for persistent application data storage.
- **Razor Views** and **HTML/CSS/Bootstrap** for the user interface.
- **Cookie Authentication** for secure user authentication and session management.
- **Role-Based Authorization Policies** for controlling access to protected areas of the system.
- **xUnit** for automated unit and integration testing.
- **GitHub Actions** for continuous integration and continuous deployment.
- **Microsoft Azure** for cloud hosting and production deployment.

### 2. Application Structure

The application follows a structured MVC and layered architecture to separate responsibilities and improve maintainability.

- **Controllers** handle HTTP requests, authorization, validation, and application flow.
- **Services** contain business logic and coordinate operations between controllers and the database.
- **Models** represent the application's domain entities and data structures.
- **Data** contains the Entity Framework Core database context, migrations, and database seeding functionality.
- **Views** provide the user interface using Razor syntax.
- **wwwroot** contains static assets such as CSS, JavaScript, and images.
- **Tests** contains automated unit and integration tests used to verify application functionality.

This separation of concerns allows individual components to be maintained, tested, and extended without unnecessarily affecting other areas of the system.

### 3. Database

The system uses **Microsoft SQL Server** with **Entity Framework Core** to manage persistent application data.

The database stores information relating to:

- Users and authentication accounts
- Patients
- Doctors
- Receptionists
- Appointments
- Consultations
- Medical records
- Prescriptions
- Other healthcare-related application data

`ApplicationDbContext` manages the application's entity relationships, database configuration, and data access.

Entity Framework Core migrations are used to version and apply database schema changes. Database constraints and indexes are used to support data integrity, efficient querying, and reliable relationships between entities.

### 4. Authentication & Security

Security is implemented using ASP.NET Core's authentication and authorization infrastructure.

The system uses:

- Secure password hashing through ASP.NET Core's `PasswordHasher<User>`.
- Cookie-based authentication for authenticated sessions.
- Role-based authorization policies for protected application areas.
- Administrator, Doctor, Receptionist, and Patient role separation.
- Anti-forgery protection for form submissions.
- Authentication cookies configured with appropriate security settings.
- Login and access-denied handling for protected resources.
- Active-user validation to prevent inactive accounts from accessing protected functionality.
- Secure handling of return URLs to prevent external redirects after authentication.
- Input validation and server-side validation for user-provided data.

Sensitive authentication information is not stored in plain text. Passwords are hashed before being persisted to the database.

### 5. API & Backend

The backend provides the application's core business logic and data-processing functionality through controllers and service classes.

The service layer manages operations such as:

- User management
- Patient management
- Doctor management
- Appointment management
- Consultation management
- Medical record management
- Prescription management
- Authentication and authorization-related operations

Controllers are responsible for receiving requests and returning the appropriate views or responses, while services encapsulate reusable business rules and database operations.

Validation and error-handling mechanisms are implemented to ensure that invalid requests and application errors are handled consistently.

### 6. Frontend

The frontend is implemented using **Razor Views, HTML, CSS, JavaScript, and Bootstrap**.

The interface provides separate experiences for the different user roles and includes:

- Responsive navigation
- Role-specific dashboards
- Authentication pages
- Patient management interfaces
- Doctor management interfaces
- Reception and appointment management interfaces
- Administrator management interfaces
- Consultation and medical information interfaces
- Form validation and user feedback
- Responsive layouts for different screen sizes

The application uses a shared layout to maintain consistent navigation, branding, styling, and user-interface structure across the system.

### 7. Testing

The project includes an automated test suite covering core application functionality.

Testing includes:

- Unit testing of application services and business logic.
- Controller testing.
- Authentication and authorization testing.
- Integration testing of important application workflows.
- Validation of valid and invalid authentication scenarios.
- Database-related application testing using an isolated test environment.

The automated test suite is integrated into the development workflow so that application changes can be verified before deployment.

### 8. CI/CD Pipeline

GitHub Actions is used to automate the application's continuous integration and continuous deployment workflow.

The CI/CD process includes:

- Restoring project dependencies.
- Building the ASP.NET Core application.
- Running the automated test suite.
- Validating the application before deployment.
- Publishing the application for deployment.
- Deploying the application to the Azure hosting environment.

Automating these stages provides a repeatable deployment process and reduces the need for manual build and deployment steps.

### 9. GitHub Branching Strategy

The project uses a structured Git branching strategy to separate development work from production code.

The primary branches are:

- **Feature branches** are used for developing individual features or making isolated changes.
- **Develop** is used to integrate completed development work before production release.
- **Main** represents the production-ready version of the application.

Pull requests are used to review and merge changes between branches. Branch protection and repository rules are used to support controlled changes to important branches.

This workflow provides traceability of changes while reducing the risk of unreviewed changes being introduced directly into the production branch.

### 10. Azure Hosting & Deployment

The Smart Clinic Management System is deployed to **Microsoft Azure** to provide a cloud-based production environment.

The Azure deployment provides:

- Cloud hosting for the ASP.NET Core application.
- Connectivity between the deployed application and its database.
- Production access through the deployed web application.
- Automated deployment through GitHub Actions.
- Database initialization and migration support during application startup.
- Application configuration through the Azure hosting environment.

The production application is designed to provide a stable and repeatable deployment process, with automated build, testing, and deployment stages reducing the dependency on manual deployment procedures.

---

## System Requirements

The Smart Clinic Management System requires the following environment for local development and execution:

- **Operating System:** Windows 10 or Windows 11.
- **.NET SDK:** .NET version supported by the project.
- **IDE:** Visual Studio 2022 or another compatible C# development environment.
- **Database:** Microsoft SQL Server or SQL Server LocalDB.
- **Git:** Required for cloning and managing the project repository.
- **Web Browser:** A modern browser such as Microsoft Edge, Google Chrome, or Mozilla Firefox.
- **Internet Connection:** Required for retrieving project dependencies and accessing the deployed Azure application.

---

## Local Setup

Follow these steps to run the application locally:

1. Clone the repository from GitHub.
2. Open the solution in Visual Studio.
3. Restore the required NuGet packages.
4. Configure the database connection string in the application's configuration.
5. Ensure SQL Server or SQL Server LocalDB is available.
6. Build the solution to verify that the project compiles successfully.
7. Run the application from Visual Studio or using the .NET CLI.
8. During application startup, the database is initialized and required seed data is created.
9. Open the local application URL displayed by Visual Studio or the .NET development server.
10. Sign in using one of the available demo accounts.

Entity Framework Core migrations are used to create and update the database schema when the application starts in a relational database environment.

---

## Demo Accounts

The application includes seeded demonstration accounts for the main system roles.

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@smartclinic.local` | `Admin123!` |
| Doctor | `doctor@smartclinic.local` | `Doctor123!` |
| Receptionist | `reception@smartclinic.local` | `Reception123!` |
| Patient | `patient@smartclinic.local` | `Patient123!` |

These accounts are intended for demonstration and testing of the role-specific functionality provided by the system.

---

## Live Application

The Smart Clinic Management System is deployed to Microsoft Azure and can be accessed through the live production environment.

The deployed application provides access to the same core functionality available in the local development environment, including:

- Secure user authentication.
- Role-based access control.
- Administrator functionality.
- Doctor functionality.
- Receptionist functionality.
- Patient functionality.
- Appointment management.
- Consultation and medical information functionality.

The production environment is deployed through the project's CI/CD pipeline, allowing validated application changes to be built, tested, and deployed to Azure.

---

## Future Enhancements

The Smart Clinic Management System provides the core functionality required for managing clinic operations. Potential future enhancements include:

- Online appointment reminders through email or SMS notifications.
- Enhanced patient communication and notification functionality.
- Expanded reporting and analytics for clinic administrators.
- Additional dashboard visualisations for operational and clinical information.
- Integration with external healthcare and laboratory systems.
- More advanced appointment scheduling and availability management.
- Improved document and medical-record management.
- Additional audit logging and monitoring capabilities.
- Mobile application support for patients and healthcare staff.
- Further automation of administrative and clinical workflows..

---

  ## Project Links

| Resource | Link |
|---|---|
| GitHub Web App | https://github.com/shahaanp19/Smart-Clinic.git  |
| YouTube | [YouTube](PASTE_YOUTUBE_LINK_HERE) |
| Live Web App | https://smartclinicmanagementsystem-apd0d2gqb6ftakbv.southafricanorth-01.azurewebsites.net/ |
| Live Mobile App | [Live Mobile App](PASTE_LIVE_MOBILE_APP_LINK_HERE) |
| GitHub Mobile App | [GitHub Mobile App](PASTE_GITHUB_MOBILE_APP_LINK_HERE) |
