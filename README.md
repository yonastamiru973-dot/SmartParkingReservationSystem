# Smart Parking Reservation System

A web application for reserving and managing parking spots, built with **ASP.NET Core MVC 8.0**, **Entity Framework Core**, and **SQL Server**.

This repository currently implements **Sprint 1 — Project Setup & User Authentication**.

---

## Sprint 1 — what's included

User stories covered:

| ID    | User Story                                                                            |
| ----- | ------------------------------------------------------------------------------------- |
| US1.1 | As a user, I want to register an account so that I can access the parking system.    |
| US1.2 | As a user, I want to log in securely so that my data is protected.                   |
| US1.3 | As a user, I want to reset my password if I forget it.                               |
| US1.4 | As an admin, I want to view all registered users so that I can manage them.          |

Acceptance criteria mapping:

| AC#   | Acceptance Criteria                                                            | Where it's implemented                                                                       |
| ----- | ------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------- |
| AC1.1 | Register with name, email, phone, password, vehicle plate                      | `Views/Account/Register.cshtml`, `RegisterViewModel`, `UserService.RegisterAsync`            |
| AC1.2 | Email format + password ≥ 6 chars validation                                   | `RegisterViewModel` data annotations                                                          |
| AC1.3 | Duplicate-email rejection                                                       | `UserService.RegisterAsync` + unique index in `ApplicationDbContext`                          |
| AC1.4 | Login with correct credentials                                                  | `AccountController.Login`                                                                     |
| AC1.5 | Error on wrong credentials                                                      | `AccountController.Login` returns generic "Invalid email or password."                       |
| AC1.6 | Redirect to dashboard after login                                               | `AccountController.Login` → `HomeController.Dashboard`                                        |
| AC1.7 | Password reset via email                                                        | `AccountController.ForgotPassword` + `EmailService`                                           |
| AC1.8 | Reset link expires after 1 hour                                                 | `PasswordResetToken.ExpiresAt` + `UserService.CreatePasswordResetTokenAsync`                  |
| AC1.9 | Admin can view all users                                                        | `AdminController.Users` (role-gated by `SessionAuthorize(Roles="Admin")`)                     |
| AC1.10 | Profile shows user info                                                        | `ProfileController.Index`                                                                     |
| AC1.11 | Profile is editable                                                            | `ProfileController.Edit`                                                                      |
| AC1.12 | Passwords hashed                                                               | `PasswordHasher` (BCrypt, work factor 11)                                                     |
| AC1.13 | Runs without crashes on localhost                                              | Auto-migrates / seeds admin on startup                                                        |

---

## Tech stack

- **ASP.NET Core 8.0 MVC** — Controllers, Views (Razor), Models
- **Entity Framework Core 8** — `Microsoft.EntityFrameworkCore.SqlServer`
- **SQL Server LocalDB** — default connection string (overridable in `appsettings.json`)
- **BCrypt.Net-Next** — secure password hashing
- **ASP.NET Core Session** — server-side session for authentication state
- Built-in **data annotation** validation + **jQuery unobtrusive** client-side validation

---

## Project structure

```
ParkingManagementSystem/
├── Controllers/
│   ├── HomeController.cs        # Landing page & dashboard
│   ├── AccountController.cs     # Register, Login, Logout, Forgot/Reset Password
│   ├── ProfileController.cs     # View/edit own profile
│   └── AdminController.cs       # Admin-only: list all users
├── Models/
│   ├── User.cs
│   ├── PasswordResetToken.cs
│   ├── ErrorViewModel.cs
│   └── ViewModels/
│       ├── RegisterViewModel.cs
│       ├── LoginViewModel.cs
│       ├── ForgotPasswordViewModel.cs
│       ├── ResetPasswordViewModel.cs
│       └── ProfileViewModel.cs
├── Data/
│   ├── ApplicationDbContext.cs  # EF Core DbContext + model configuration
│   └── DbSeeder.cs              # Creates default admin on startup
├── Services/
│   ├── IUserService.cs / UserService.cs       # Registration, login validation, profile, reset tokens
│   ├── IPasswordHasher.cs / PasswordHasher.cs # BCrypt hashing
│   ├── IEmailService.cs / EmailService.cs     # Dev: file log; Prod: SMTP
│   └── ICurrentUserService.cs                 # Session-backed current user info
├── Filters/
│   └── SessionAuthorizeAttribute.cs           # Session-based authorization filter
├── Views/
│   ├── _ViewImports.cshtml / _ViewStart.cshtml
│   ├── Shared/_Layout.cshtml + Error.cshtml + _ValidationScriptsPartial.cshtml
│   ├── Home/{Index, Dashboard, Privacy}.cshtml
│   ├── Account/{Register, Login, ForgotPassword, ForgotPasswordConfirmation, ResetPassword, AccessDenied}.cshtml
│   ├── Profile/{Index, Edit}.cshtml
│   └── Admin/Users.cshtml
├── wwwroot/
│   ├── css/site.css
│   └── lib/  (jQuery + jQuery validation)
├── App_Data/                    # Dev mode: password-reset-emails.log
├── Program.cs                   # DI, EF Core, session, seeding, MVC routing
├── appsettings.json             # Connection string + app settings + email settings
└── ParkingManagementSystem.csproj
```

---

## Database schema

Created via `EF Core` with `Database.EnsureCreated()` on startup (no migrations required for Sprint 1).

### `Users`

| Column              | Type                | Notes                                                |
| ------------------- | ------------------- | ---------------------------------------------------- |
| `Id`                | INT, PK, Identity   |                                                      |
| `FullName`          | NVARCHAR(100)       | Required                                             |
| `Email`             | NVARCHAR(150)       | Required, **unique index**                           |
| `PhoneNumber`       | NVARCHAR(20)        | Required                                             |
| `VehiclePlateNumber`| NVARCHAR(20)        | Required, stored upper-cased                         |
| `PasswordHash`      | NVARCHAR(MAX)       | Required (BCrypt hash, never plain text)             |
| `Role`              | NVARCHAR(20)        | Default `"User"`; `"Admin"` for admins               |
| `CreatedAt`         | DATETIME2 (UTC)     |                                                      |
| `UpdatedAt`         | DATETIME2 NULL      |                                                      |
| `IsActive`          | BIT                 | Default true                                         |

### `PasswordResetTokens`

| Column      | Type                | Notes                                                                  |
| ----------- | ------------------- | ---------------------------------------------------------------------- |
| `Id`        | INT, PK, Identity   |                                                                        |
| `UserId`    | INT, FK → Users.Id  | Cascade delete                                                         |
| `Token`     | NVARCHAR(128)       | URL-safe random 48-byte token, **unique index**                        |
| `ExpiresAt` | DATETIME2 (UTC)     | `CreatedAt + AppSettings:PasswordResetTokenLifetimeHours` (default 1h) |
| `CreatedAt` | DATETIME2 (UTC)     |                                                                        |
| `Used`      | BIT                 | Single-use; existing unused tokens are invalidated on new requests     |

### Default seed data

On first startup the app seeds a default administrator (if no admin exists):

- **Email:** `admin@parking.local`
- **Password:** `Admin@123`

> **Change this password after first login in any non-development environment.**

---

## Running locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server **LocalDB** (ships with Visual Studio) — or update the connection string in `appsettings.json` to point at any SQL Server instance.

### From Visual Studio

1. Open `ParkingManagementSystem.sln`.
2. Set `ParkingManagementSystem` as the startup project.
3. Press **F5**.

The database is created automatically on first run, and a default admin user is seeded.

### From the command line

```bash
cd ParkingManagementSystem
dotnet restore
dotnet run
```

Then open https://localhost:7133 (or whichever URL the host prints).

### Optional: configure SMTP for real password-reset emails

Edit `appsettings.json`:

```json
"EmailSettings": {
  "Mode": "Smtp",
  "FromAddress": "no-reply@yourdomain.com",
  "FromName": "Parking Management System",
  "SmtpHost": "smtp.yourprovider.com",
  "SmtpPort": 587,
  "SmtpUser": "your-username",
  "SmtpPassword": "your-password",
  "EnableSsl": true
}
```

In **Development** mode (default), no email is actually sent — instead the reset link is written to:

```
ParkingManagementSystem/App_Data/password-reset-emails.log
```

This makes Sprint 1 testable end-to-end without configuring an SMTP server.

---

## Manual test plan (Sprint 1)

| Test  | Steps                                                            | Expected                                        |
| ----- | ---------------------------------------------------------------- | ----------------------------------------------- |
| T1.1  | Register with valid data                                          | Account created, redirect to login              |
| T1.2  | Register again with the same email                                | Error: "An account with this email already exists" |
| T1.3  | Register with `not-an-email`                                      | Email format validation error                   |
| T1.4  | Register with password `12345`                                    | "Password must be at least 6 characters long"   |
| T1.5  | Log in with the correct credentials                               | Redirects to `/Home/Dashboard`                  |
| T1.6  | Log in with the wrong password                                    | "Invalid email or password."                    |
| T1.7  | Log in with a non-existent email                                  | "Invalid email or password."                    |
| T1.8  | Visit `/Profile` (or `/Home/Dashboard`) while logged out          | Redirects to `/Account/Login?returnUrl=...`     |
| T1.9  | Request password reset for a registered email                     | Confirmation shown; link in `App_Data/password-reset-emails.log` |
| T1.10 | Request password reset for an unknown email                       | "No account is associated with that email"      |
| T1.11 | Use a reset link older than 1 hour (or after using it)            | "This reset link has expired."                  |
| T1.12 | Log in as `admin@parking.local` and open `/Admin/Users`           | Table of all registered users                   |
| T1.13 | Edit profile (name/phone/plate) and save                          | "Profile updated successfully." + values persisted |

---

## Security notes

- All passwords are hashed with **BCrypt** (`PasswordHasher`). Plain-text passwords never touch the database.
- Password reset tokens are 48 cryptographically-random bytes, URL-safe base64-encoded, single-use, and expire in 1 hour.
- Anti-forgery tokens are validated on all state-changing POSTs (registration, login, logout, password reset, profile edit).
- Session cookie is `HttpOnly` and marked essential. Idle timeout: 60 minutes.
- Unauthenticated access to authenticated areas is redirected to the login page with a `returnUrl`.
- Admin-only areas are gated by a role check on the session-backed identity.

---

## Definition of Done — Sprint 1

- [x] All code committed to version control (Git)
- [x] All acceptance criteria implemented
- [x] Manual test plan documented above
- [x] No critical/major bugs
- [x] Database schema documented (this README)
- [ ] Code reviewed by team member *(team activity)*
- [ ] Sprint retrospective completed *(team activity)*
