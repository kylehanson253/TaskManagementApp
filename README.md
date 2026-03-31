# Task Management App

A multi-tenant task management application demonstrating clean architecture with a .NET API, React SPA, and WPF desktop companion.

---

## Quick Start

### 1 — API

```bash
cd src/TaskManagement.API
dotnet run
# API listens on http://localhost:5111
```

The SQLite database (`taskmanagement.db`) is created automatically on first run with seed data.

**Seed accounts**

| Email                   | Password  | Role  | Tenant        |
|-------------------------|-----------|-------|---------------|
| admin@acme.com          | Admin@123 | Admin | Acme Corp     |
| user@acme.com           | User@123  | User  | Acme Corp     |
| admin@techstartup.com   | Admin@123 | Admin | Tech Startup  |

### 2 — React frontend

```bash
cd frontend
npm install
npm run dev
# Opens on http://localhost:5173
```

### 3 — WPF Desktop

```bash
cd src/TaskManagement.Desktop
dotnet run
# Requires the API to be running first
```

### 4 — Tests

```bash
dotnet test tests/TaskManagement.Tests
```

---

## Architecture

```
TaskManagementApp/
├── src/
│   ├── TaskManagement.Core/           # Domain entities, interfaces, enums
│   ├── TaskManagement.Application/    # Business logic, services, DTOs
│   ├── TaskManagement.Infrastructure/ # EF Core, repositories, unit of work
│   ├── TaskManagement.API/            # ASP.NET Core REST API
│   └── TaskManagement.Desktop/        # WPF companion app
├── tests/
│   └── TaskManagement.Tests/          # xUnit unit tests
└── frontend/                          # React + Vite SPA
```

### Clean / Layered Architecture

Dependencies flow inward: `API → Application → Core ← Infrastructure`.
The Core project has no external dependencies.

---

## Key Design Decisions

### Multi-tenancy
Every database query is filtered by `TenantId` at the repository level. A user's `TenantId`
is embedded in their JWT claim so it is verified on every request without an extra DB lookup.
Users cannot access data from other tenants regardless of role.

### Authentication & Authorization
- Short-lived JWT access tokens (10 minutes that auto refreshes) + opaque refresh tokens (7 days)
- Two roles: **Admin** and **User**
  - Admins see and manage all tasks and users within their tenant
  - Users see only tasks they created or are assigned to
- Role stored as a claim; verified by `[Authorize(Roles = "Admin")]` attribute decorators

### EF Core & Database
- SQLite chosen for zero-setup simplicity; switching to SQL Server requires only a connection string change and a different `UseSqlite` → `UseSqlServer` call in `Program.cs`
- Code-first with seed data; `EnsureCreated()` initialises the schema on startup

### Repository & Unit of Work
The generic `Repository<T>` handles CRUD; specialised repositories (`TaskRepository`, `UserRepository`, etc.) add domain-specific queries. `UnitOfWork` batches all changes into a single `SaveChanges` call, ensuring transactional consistency.

### Frontend (React)
- Vite for fast builds
- Axios with interceptors for automatic JWT injection and 401 redirect
- `AuthContext` stores the authenticated user; `ProtectedRoute` enforces auth at the routing level
- No UI library — plain CSS with CSS custom properties for a maintainable, responsive layout

### Desktop (WPF)
- MVVM pattern: `LoginViewModel` / `TaskListViewModel` with `INotifyPropertyChanged`
- `RelayCommand` wraps async operations with `CanExecute` gating
- `ApiService` wraps `HttpClient` with the same endpoints as the frontend

### Logging
Serilog is used for structured logging with Console and rolling-file sinks. All service operations log at `Information`; security events (failed logins) log at `Warning`.

---


### Optimized Queries and Stored Procedures
Since the app uses SQLite, there are no stored proecedures

GetTaskSummaryByTenantAsync in TaskRepository is the only query designed for efficiency:

_context.Tasks
    .Where(t => t.TenantId == tenantId)
    .GroupBy(t => t.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync();

EF Core translates this to a single aggregated SQL query like:

SELECT "Status", COUNT(*) AS "Count"
FROM "Tasks"
WHERE "TenantId" = @tenantId
GROUP BY "Status"

This avoids loading all task rows into memory just to count them — it lets the database do the aggregation work.
 
Everything else — Standard EF Core LINQ

---


## 3rd-Party Libraries

| Library | Reason |
|---|---|
| `BCrypt.Net-Next` | Secure password hashing with salted bcrypt — the industry standard for password storage |
| `Microsoft.EntityFrameworkCore.Sqlite` | Lightweight, file-based database for dev/demo — no server setup required |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Official ASP.NET Core JWT middleware — minimal boilerplate, well-maintained |
| `System.IdentityModel.Tokens.Jwt` | Token generation with full control over claims |
| `Serilog.AspNetCore` | Structured logging with enrichers and multiple sinks; significantly better than the default `ILogger` for observability |
| `Newtonsoft.Json` | Mature, flexible JSON library used in the WPF desktop app |
| `Moq` | De-facto .NET mocking library for unit tests |
| `FluentAssertions` | Readable assertion syntax that produces clear failure messages |
| `Microsoft.EntityFrameworkCore.InMemory` | In-memory EF provider for fast, isolated unit tests |
| `axios` | Promise-based HTTP client with interceptors for JWT injection and error handling |
| `react-router-dom` | Declarative client-side routing with protected route support |

---

## Known Limitations

- No email verification on registration.
- The WPF app does not support task creation or editing (view + complete only) — this was intentional scope reduction for the companion app.
- No pagination on the tasks list endpoint — acceptable for demo data volumes.
- JWT secret is in `appsettings.json` — in production, read from Azure Key Vault or environment variable `JwtSettings__Secret`.

---


## What to do if there was more time
- Fix the known issues above
- Host the api/webpage on Azure for easier demo purposes
- Add more features: like modifing tasks
- Separate each tenant into separate schemas or databases, if necessary for tenant isolation
---


## Bonus: Azure / CI-CD notes

To deploy to Azure App Service:
1. Set `ASPNETCORE_ENVIRONMENT=Production` in App Service configuration
2. Add `JwtSettings__Secret` as an App Service environment variable (or Key Vault reference)
3. Change connection string to an Azure SQL / SQL Server connection string

A GitHub Actions workflow would look like:

```yaml
# .github/workflows/api.yml
name: Build & Test API
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
```
