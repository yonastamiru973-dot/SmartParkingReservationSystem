# Smart Parking Reservation System

A web application for reserving and managing parking spots, built with **ASP.NET Core MVC 8.0**, **Entity Framework Core**, and **SQL Server**.

This repository currently implements **Sprint 1 — Project Setup & User Authentication**, **Sprint 2 — Parking Slot Management & Availability**, and **Sprint 3 — Reservation System & Time Tracking**.

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

## Sprint 2 — what's included

User stories covered:

| ID    | User Story                                                                          |
| ----- | ----------------------------------------------------------------------------------- |
| US2.1 | As an admin, I want to add, edit, and delete parking slots.                        |
| US2.2 | As a user, I want to view all available parking slots.                             |
| US2.3 | As a user, I want to see parking slots on a map.                                   |
| US2.4 | As an admin, I want to mark a slot as available, occupied, or maintenance.         |

Acceptance criteria mapping:

| AC#    | Acceptance Criteria                                                       | Where it's implemented                                                          |
| ------ | ------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| AC2.1  | Admin can add a slot with number, type, hourly rate, and coordinates      | `AdminSlotsController.Create` + `Views/AdminSlots/Create.cshtml`               |
| AC2.2  | Admin can edit slot details                                               | `AdminSlotsController.Edit`                                                     |
| AC2.3  | Admin can delete (soft delete)                                            | `AdminSlotsController.Delete` → `ParkingSlotService.SoftDeleteAsync` sets `IsDeleted=true` |
| AC2.4  | User views slots in a grid                                                | `Views/Slots/Index.cshtml` + `_SlotCard.cshtml`                                 |
| AC2.5  | Each slot card shows number, type, status, hourly rate                    | `_SlotCard.cshtml`                                                              |
| AC2.6  | Available = green                                                         | `.slot-card.status-available` in `site.css`                                     |
| AC2.7  | Occupied = red                                                            | `.slot-card.status-occupied`                                                    |
| AC2.8  | Maintenance = yellow                                                      | `.slot-card.status-maintenance`                                                 |
| AC2.9  | Slots are shown on a map-style view as positioned tiles                   | Simulated **parking-lot floor view** in `slots.js` (`renderMap()`)              |
| AC2.10 | Clicking a tile shows slot details                                        | In-page popover built by `slots.js` `buildPopoverContent()` / `openPopover()`   |
| AC2.11 | Admin can manually change status                                          | Inline dropdown on `AdminSlots/Index` → `AdminSlotsController.SetStatus` (AJAX) |
| AC2.12 | Filter by type (Standard, VIP, EV)                                        | `SlotsController.Index` + filter bar form                                       |
| AC2.13 | Search by slot number                                                     | `SlotsController.Index` + `IParkingSlotService.SearchAsync`                     |
| AC2.14 | Slot availability updates without a refresh                               | `SlotsController.Status` JSON endpoint polled every 5s by `slots.js`            |

### Sample data

`DbSeeder.SeedSampleSlots` inserts 10 sample slots covering all 3 types (Standard / VIP / EV) and all 3 statuses (Available / Occupied / Maintenance) clustered around the map's default center, so the page is immediately populated after first run.

### Simulated parking-lot view (no external map service)

Sprint 2 ships with a **fully self-contained "floor-view" simulation** instead of an external map service like Google Maps:

- Each slot is rendered as a color-coded tile on a stylized top-down lot, with its position derived from the slot's `Latitude` / `Longitude` (normalized to the bounding box of all visible slots).
- Tile colors track the live status (green = Available, red = Occupied, amber = Maintenance).
- Clicking a tile opens an in-page popover with the slot's number, status, type, hourly rate, optional notes, and coordinates — exactly mirroring the data you'd see in a Maps InfoWindow.
- The same view auto-refreshes via AJAX every 5 seconds, so the lot, the grid cards, and the count pills all stay in sync without a reload.

This means **no API key, no external script, no internet connection required**.

The default lat/lng used to pre-fill the admin form is configurable:

```json
"ParkingLot": {
  "DefaultLatitude": 40.7589,
  "DefaultLongitude": -73.9851
}
```

You can change these to your campus / facility's coordinates — they're stored on every slot and used to place tiles in the floor view.

### Upgrading an existing Sprint 1 database

If you ran Sprint 1 earlier, your database already exists and `EnsureCreated()` will not add the new `ParkingSlots` table on its own. The seeder calls `EnsureParkingSlotsTable()` which runs an idempotent `IF NOT EXISTS … CREATE TABLE …` to add the table the first time you run Sprint 2 — no manual migration step is needed.

---

## Sprint 3 — what's included

User stories covered:

| ID    | User Story                                                                          |
| ----- | ----------------------------------------------------------------------------------- |
| US3.1 | As a user, I want to reserve an available parking slot for a specific date and time. |
| US3.2 | As a user, I want to view my active and past reservations.                         |
| US3.3 | As a user, I want to cancel a reservation before the start time.                    |
| US3.4 | As a user, I want to receive a QR code after successful reservation.                |
| US3.5 | As an admin, I want to view all reservations and their statuses.                    |
| US3.6 | As a user, I want to extend my parking time remotely.                                |

Acceptance criteria mapping:

| AC#    | Acceptance Criteria                                                       | Where it's implemented                                                          |
| ------ | ------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| AC3.1  | Select slot, date, start time, duration (30 min – 24 h)                   | `Reservations/Create` + `CreateReservationViewModel`                            |
| AC3.2  | Estimated fee shown before confirmation                                   | `CreateReservationViewModel.EstimatedFee` + `reservation-create.js`           |
| AC3.3  | Double-booking prevented                                                  | `ReservationService.CreateAsync` (SERIALIZABLE tx + overlap query)              |
| AC3.4  | Past start time rejected                                                  | `ReservationService.CreateAsync` + model validation                             |
| AC3.5  | Unique QR code generated after confirmation                               | `QrCodeService.GenerateSvg` on `Reservations/Details`                             |
| AC3.6  | QR contains reservation ID, user ID, slot ID, times, HMAC signature       | `QrCodeService.CreateToken` → `RES\|{id}\|{user}\|{slot}\|{ticks}\|{sig}`       |
| AC3.7  | QR expires 15 minutes after start time                                    | `ProcessEntryScanAsync` + `Reservations:QrGracePeriodMinutes`                   |
| AC3.8  | User views active reservations                                            | `Reservations/Index` → Active tab                                               |
| AC3.9  | User views past reservations with duration and fee                        | `Reservations/Index` → Past tab + `_ReservationRow`                             |
| AC3.10 | Cancel up to 15 minutes before start                                      | `ReservationService.CancelAsync` + `Reservations:CancelWindowMinutes`           |
| AC3.11 | Admin views all reservations with filters                                 | `AdminReservations/Index` (date, status, user search)                         |
| AC3.12 | Attendant scans QR to validate entry                                      | `Scanner/Index` + `ProcessEntryScanAsync`                                       |
| AC3.13 | Scanner shows user name, slot number, valid time window                   | `Scanner/Index` scan result banner                                              |
| AC3.14 | Entry time logged on scan                                                 | `ProcessEntryScanAsync` sets `EntryTime` + `Status = Active`                    |
| AC3.15 | Actual duration calculated on exit                                        | `ProcessExitScanAsync` sets `ActualDuration = ExitTime - EntryTime`             |
| AC3.16 | Extend parking before expiry                                              | `Reservations/Extend` + `ReservationService.ExtendAsync`                        |
| AC3.17 | Extension fee calculated correctly                                        | `PricingService.CalculateFee` applied to additional minutes                     |

### QR code format

Each QR encodes a self-contained HMAC-SHA256 signed token:

```
RES|{reservationId}|{userId}|{slotId}|{startTicks}|{endTicks}|{signature}
```

- Generated with **QRCoder** (SVG output).
- Verified on every scan; tampered tokens are rejected.
- Set `Reservations:QrSecret` in `appsettings.json` to a long random value in production.

### Automatic status transitions

`ReservationStatusUpdater` (background service, runs every 60 s):

| From       | To        | Condition                                              |
| ---------- | --------- | ------------------------------------------------------ |
| Confirmed  | Expired   | Start time + 15 min grace passed with no entry scan    |
| Active     | Completed | Planned `EndTime` passed (auto-exit if no exit scan)   |

### Scanner

`/scanner` (admin role) provides:

- **Camera scan** via `html5-qrcode` (works on mobile + desktop webcam).
- **Manual token paste** fallback when camera is unavailable.
- Entry / Exit toggle for attendants.

---

## Tech stack

- **ASP.NET Core 8.0 MVC** — Controllers, Views (Razor), Models
- **Entity Framework Core 8** — `Microsoft.EntityFrameworkCore.SqlServer`
- **SQL Server LocalDB** — default connection string (overridable in `appsettings.json`)
- **BCrypt.Net-Next** — secure password hashing
- **QRCoder** — QR code SVG generation for reservation entry
- **html5-qrcode** — camera-based QR scanning for attendants
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
│   ├── AdminController.cs       # Admin-only: list all users
│   ├── SlotsController.cs       # Public slot listing + JSON live-status feed
│   ├── AdminSlotsController.cs  # Admin-only slot CRUD + status toggle
│   ├── ReservationsController.cs # User reservation CRUD, cancel, extend
│   ├── AdminReservationsController.cs # Admin reservation list + filters
│   └── ScannerController.cs     # QR entry/exit scanner for attendants
├── Models/
│   ├── User.cs
│   ├── PasswordResetToken.cs
│   ├── ParkingSlot.cs
│   ├── Reservation.cs
│   ├── Enums/{SlotType, SlotStatus, ReservationStatus}.cs
│   ├── ErrorViewModel.cs
│   └── ViewModels/
│       ├── RegisterViewModel.cs … ProfileViewModel.cs
│       ├── ParkingSlotFormViewModel.cs / SlotsIndexViewModel.cs
│       ├── CreateReservationViewModel.cs / ReservationDetailsViewModel.cs
│       ├── MyReservationsViewModel.cs / ExtendReservationViewModel.cs
│       ├── AdminReservationsViewModel.cs / ScannerViewModel.cs
├── Data/
│   ├── ApplicationDbContext.cs  # EF Core DbContext + model configuration
│   └── DbSeeder.cs              # Creates default admin on startup
├── Services/
│   ├── IUserService.cs / UserService.cs       # Registration, login validation, profile, reset tokens
│   ├── IPasswordHasher.cs / PasswordHasher.cs # BCrypt hashing
│   ├── IEmailService.cs / EmailService.cs     # Dev: file log; Prod: SMTP
│   ├── ICurrentUserService.cs                 # Session-backed current user info
│   ├── IParkingSlotService.cs / ParkingSlotService.cs # Slot CRUD, search/filter, status updates
│   ├── IReservationService.cs / ReservationService.cs # Booking, cancel, extend, scan, status sweep
│   ├── IQrCodeService.cs / QrCodeService.cs   # HMAC-signed QR tokens + SVG generation
│   ├── IPricingService.cs / PricingService.cs # Hourly fee calculation
│   └── Hosted/ReservationStatusUpdater.cs   # Background status auto-transition
├── Filters/
│   └── SessionAuthorizeAttribute.cs           # Session-based authorization filter
├── Views/
│   ├── _ViewImports.cshtml / _ViewStart.cshtml
│   ├── Shared/_Layout.cshtml + Error.cshtml + _ValidationScriptsPartial.cshtml
│   ├── Home/{Index, Dashboard, Privacy}.cshtml
│   ├── Account/{Register, Login, ForgotPassword, ForgotPasswordConfirmation, ResetPassword, AccessDenied}.cshtml
│   ├── Profile/{Index, Edit}.cshtml
│   ├── Admin/Users.cshtml
│   ├── Slots/{Index, _SlotCard}.cshtml
│   ├── AdminSlots/{Index, Create, Edit, Delete}.cshtml
│   ├── Reservations/{Index, Create, Details, Cancel, Extend, _ReservationRow}.cshtml
│   ├── AdminReservations/Index.cshtml
│   └── Scanner/Index.cshtml
├── wwwroot/
│   ├── css/site.css
│   ├── js/{slots.js, admin-slots.js, reservations.js, reservation-create.js, reservation-extend.js, scanner.js}
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

### `ParkingSlots`

| Column        | Type                | Notes                                                                            |
| ------------- | ------------------- | -------------------------------------------------------------------------------- |
| `Id`          | INT, PK, Identity   |                                                                                  |
| `SlotNumber`  | NVARCHAR(20)        | Required, stored upper-cased. **Unique among non-deleted rows** (filtered index) |
| `SlotType`    | NVARCHAR(20)        | `Standard`, `VIP`, or `EV` (stored as string)                                    |
| `Status`      | NVARCHAR(20)        | `Available`, `Occupied`, or `Maintenance` (stored as string)                     |
| `HourlyRate`  | DECIMAL(10,2)       | Must be > 0                                                                      |
| `Latitude`    | FLOAT               | Range -90..90                                                                    |
| `Longitude`   | FLOAT               | Range -180..180                                                                  |
| `Description` | NVARCHAR(500) NULL  |                                                                                  |
| `IsDeleted`   | BIT                 | Soft-delete flag; deleted slots are hidden from all queries                      |
| `CreatedAt`   | DATETIME2 (UTC)     |                                                                                  |
| `UpdatedAt`   | DATETIME2 NULL      |                                                                                  |
| `DeletedAt`   | DATETIME2 NULL      |                                                                                  |

### `Reservations`

| Column           | Type                | Notes                                                                 |
| ---------------- | ------------------- | --------------------------------------------------------------------- |
| `Id`             | INT, PK, Identity   |                                                                       |
| `UserId`         | INT, FK → Users     | Restrict delete                                                       |
| `SlotId`         | INT, FK → ParkingSlots | Restrict delete                                                    |
| `StartTime`      | DATETIME2           | Planned start (local)                                                 |
| `EndTime`        | DATETIME2           | Planned end (local); updated on extension                             |
| `Status`         | NVARCHAR(20)        | `Confirmed`, `Active`, `Completed`, `Cancelled`, `Expired`            |
| `Fee`            | DECIMAL(10,2)       | Base fee at booking                                                   |
| `ExtensionFee`   | DECIMAL(10,2)       | Cumulative extension top-ups                                          |
| `ExtensionCount` | INT                 | Number of extensions applied                                          |
| `QrToken`        | NVARCHAR(256)       | HMAC-signed token encoded in QR; **unique index**                     |
| `CreatedAt`      | DATETIME2 (UTC)     |                                                                       |
| `EntryTime`      | DATETIME2 NULL      | Set on entry scan                                                     |
| `ExitTime`       | DATETIME2 NULL      | Set on exit scan or auto-completion                                   |
| `CancelledAt`    | DATETIME2 NULL      | Set on user cancellation                                              |
| `ActualDuration` | TIME NULL           | `ExitTime - EntryTime`                                                |

**Indexes:** `(UserId, StartTime)`, `(SlotId, StartTime, EndTime, Status)`, unique `(QrToken)`.

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

### Sprint 2 manual test plan

| Test  | Steps                                                              | Expected                                                             |
| ----- | ------------------------------------------------------------------ | -------------------------------------------------------------------- |
| T2.1  | As admin, open **Manage Slots → New slot**, fill the form, submit  | Slot appears in `/admin/slots` list and on `/Slots` map + grid       |
| T2.2  | Edit a slot, change its rate/type, save                            | Changes persist and are reflected on the public page                 |
| T2.3  | Click **Delete** on a slot, confirm                                | Slot disappears from both admin list and public grid (soft-deleted)  |
| T2.4  | Visit `/Slots` as a user                                           | Grid shows all non-deleted slots, totals are correct                 |
| T2.5  | Inspect an Available slot card                                     | Green status dot, green border + label                               |
| T2.6  | Inspect an Occupied slot card                                      | Red dot/border/label                                                 |
| T2.7  | Inspect a Maintenance slot card                                    | Yellow/amber dot/border/label                                        |
| T2.8  | Refresh `/Slots`                                                    | Simulated lot view loads with a tile per slot, positioned by lat/lng |
| T2.9  | Click any tile in the lot view                                      | Popover opens with number, status badge, type, rate, description     |
| T2.10 | Apply **Type = Standard** filter                                    | Only Standard slots remain in the grid (and only their markers)      |
| T2.11 | Apply **Type = VIP** filter                                         | Only VIP slots shown                                                 |
| T2.12 | Search "A-01"                                                       | Only the matching slot is shown                                      |
| T2.13 | As admin, change a slot's status via the inline dropdown            | "Saved" feedback, color updates instantly without a page reload      |
| T2.14 | Open `/Slots` in two browsers; in admin, change a slot's status     | Both browsers reflect the new status within ≤ 5s (AJAX poll)         |

### Sprint 3 manual test plan

| Test  | Steps                                                              | Expected                                                             |
| ----- | ------------------------------------------------------------------ | -------------------------------------------------------------------- |
| T3.1  | Reserve an available slot with valid date/time/duration              | Reservation created; redirect to Details with QR                   |
| T3.2  | Reserve a slot already booked for the same window                  | "Already reserved during the selected time window" error             |
| T3.3  | Reserve with a start time in the past                              | "Start time must be in the future" validation error                  |
| T3.4  | View reservation Details after booking                             | QR code SVG displayed and scannable                                  |
| T3.5  | Decode QR / view raw token                                         | Contains reservation ID, user ID, slot ID, times, signature        |
| T3.6  | Open **My Reservations → Active** tab                              | New reservation listed with correct slot, times, fee                 |
| T3.7  | Complete/expire a reservation; check **Past** tab                  | Shows duration and total fee                                         |
| T3.8  | Cancel a Confirmed reservation > 15 min before start               | Status → Cancelled; QR invalidated                                   |
| T3.9  | Try to cancel within 15 min of start                               | "Can only be cancelled up to 15 minutes before start" error          |
| T3.10 | Admin scans valid QR (Entry) at `/scanner`                         | Success; entry time logged; status → Active                          |
| T3.11 | Scan QR after start + 15 min grace with no prior entry             | "QR code has expired" error                                          |
| T3.12 | Scan same QR again for entry after already checked in              | "Already checked in" error                                           |
| T3.13 | Scan QR (Exit) after entry                                         | Exit logged; actual duration calculated; status → Completed          |
| T3.14 | Extend an active reservation before EndTime                        | End time pushed; extension fee added; QR reissued                    |
| T3.15 | Try to extend after EndTime has passed                             | "Already ended and cannot be extended" error                         |
| T3.16 | Admin opens `/admin/reservations` with filters                     | Full list with user, slot, status, entry/exit, duration              |
| T3.17 | Two users simultaneously book the same slot/time                   | Only one succeeds (SERIALIZABLE transaction prevents double-book)    |

---

## Security notes

- All passwords are hashed with **BCrypt** (`PasswordHasher`). Plain-text passwords never touch the database.
- Password reset tokens are 48 cryptographically-random bytes, URL-safe base64-encoded, single-use, and expire in 1 hour.
- Anti-forgery tokens are validated on all state-changing POSTs (registration, login, logout, password reset, profile edit).
- Session cookie is `HttpOnly` and marked essential. Idle timeout: 60 minutes.
- Unauthenticated access to authenticated areas is redirected to the login page with a `returnUrl`.
- Admin-only areas are gated by a role check on the session-backed identity.
- Reservation QR tokens are **HMAC-SHA256 signed** (`QrCodeService`). Tampered or forged tokens are rejected on scan.
- Double-booking is prevented with a **SERIALIZABLE** database transaction and overlap query on `(SlotId, StartTime, EndTime, Status)`.
- Set `Reservations:QrSecret` to a long random value in production (never commit real secrets).

---

## Definition of Done — Sprint 1

- [x] All code committed to version control (Git)
- [x] All acceptance criteria implemented
- [x] Manual test plan documented above
- [x] No critical/major bugs
- [x] Database schema documented (this README)
- [ ] Code reviewed by team member *(team activity)*
- [ ] Sprint retrospective completed *(team activity)*

## Definition of Done — Sprint 2

- [x] All code committed to version control (Git)
- [x] All acceptance criteria implemented
- [x] Manual test plan documented above (T2.1 – T2.14)
- [x] Self-contained simulated lot view (no external map service / API key needed)
- [x] UI is responsive (filter bar collapses, lot view + grid stacks on mobile, table scrolls horizontally)
- [x] No critical/major bugs
- [x] Database seeded with 10 sample parking slots covering all types and statuses
- [ ] Code reviewed by team member *(team activity)*
- [ ] Sprint retrospective completed *(team activity)*

## Definition of Done — Sprint 3

- [x] All code committed to version control (Git)
- [x] All acceptance criteria implemented
- [x] Manual test plan documented above (T3.1 – T3.17)
- [x] QRCoder library integrated; QR SVG generated on reservation Details
- [x] Scanner interface works on mobile and desktop (camera + manual fallback)
- [x] No critical/major bugs
- [x] Database indexes on `Reservations` (user, slot+time range, unique QR token)
- [ ] Code reviewed by team member *(team activity)*
- [ ] Sprint retrospective completed *(team activity)*
