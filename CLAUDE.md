# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an **ABP Framework** application built on **ASP.NET Core 8.0** following **Domain-Driven Design (DDD)** principles. It's a layered monolith that includes a product management backend with authentication, API endpoints, and Entity Framework Core for data access.

**Primary language**: C# (.NET 8.0)
**Frontend**: Angular (client directory not shown in file listing)
**Architecture**: DDD layered monolith with ABP Framework modules

## Project Structure

The solution follows a standard ABP layered architecture:

```
src/
├── ProductManagement.Domain/              # Domain layer (entities, repositories interfaces)
│   ├── Entities/                          # Domain entities (Product, Category)
│   ├── Data/                              # Data migration interfaces
│   └── Settings/                          # Domain settings definitions
├── ProductManagement.Domain.Shared/       # Shared domain code, localization (20+ languages)
├── ProductManagement.Application/         # Application layer (use cases, app services)
│   └── AppServices/                       # Application service implementations
├── ProductManagement.Application.Contracts/  # Service contracts and DTOs (API contracts)
├── ProductManagement.HttpApi/             # API definitions and contracts
├── ProductManagement.HttpApi.Client/      # C# client for the API
├── ProductManagement.HttpApi.Host/        # API host (ASP.NET Core Web API)
│   └── Controllers/                       # API controllers
├── ProductManagement.EntityFrameworkCore/ # Data access layer (EF Core, migrations)
├── ProductManagement.AuthServer/          # Authentication server (OpenIddict)
└── ProductManagement.DbMigrator/          # Database migration/seed tool

test/
├── ProductManagement.TestBase/            # Test infrastructure base
├── ProductManagement.Application.Tests/   # Application layer tests
├── ProductManagement.Domain.Tests/        # Domain layer tests
├── ProductManagement.EntityFrameworkCore.Tests/  # Repository/integration tests
└── ProductManagement.HttpApi.Client.ConsoleTestApp/  # Console API client tests
```

**Key entity relationships**: Products belong to Categories (many-to-one relationship). Both implement `AuditedEntity<Guid>` for automatic audit fields.

## Common Development Commands

### Prerequisites
- .NET 8.0+ SDK
- Node v18 or 20 (for client-side libraries)
- Redis server (running locally for development)
- ABP CLI (`dotnet tool install -g Volo.Abp.Studio.Cli` or `abp`)

### Build
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/ProductManagement.HttpApi.Host/ProductManagement.HttpApi.Host.csproj

# Build in Release mode
dotnet build -c Release
```

### Run Applications

**Important**: Before first run, configure database connection strings in:
- `src/ProductManagement.AuthServer/appsettings.json`
- `src/ProductManagement.HttpApi.Host/appsettings.json`
- `src/ProductManagement.DbMigrator/appsettings.json`

**1. Create/Update Database** (run this first or after migrations):
```bash
dotnet run --project src/ProductManagement.DbMigrator
```

**2. Run Authentication Server** (port usually 44377 for HTTPS):
```bash
dotnet run --project src/ProductManagement.AuthServer
```

**3. Run API Host** (port usually 44333 for HTTPS):
```bash
dotnet run --project src/ProductManagement.HttpApi.Host
```

**4. Swagger UI**: Available at `https://localhost:44333/swagger` when API host is running. Supports OAuth2 with the AuthServer.

**5. Install Client Libraries** (if you add NPM packages or after pulling changes):
```bash
abp install-libs
```

### Testing

**Test Framework**: xUnit with Shouldly for assertions. ABP test infrastructure for integration tests.

**Run all tests**:
```bash
dotnet test
```

**Run tests for a specific project**:
```bash
dotnet test test/ProductManagement.Application.Tests/
dotnet test test/ProductManagement.Domain.Tests/
dotnet test test/ProductManagement.EntityFrameworkCore.Tests/
```

**Run a single test**:
```bash
dotnet test --filter "FullyQualifiedName~SampleAppServiceTests"
dotnet test --filter "FullyQualifiedName~Namespace.ClassName.TestMethod"
```

**Run tests with verbosity**:
```bash
dotnet test -v detailed
```

**Run tests in parallel** (default in xUnit, may need isolation for DB tests):
```bash
dotnet test --parallel
```

### Database Operations

**Create new migration** (after entity/model changes):
```bash
dotnet ef migrations add MigrationName --project src/ProductManagement.EntityFrameworkCore/
```

**Apply migration to database** (use DbMigrator project is preferred):
```bash
dotnet run --project src/ProductManagement.DbMigrator
```

The DbMigrator automatically applies pending migrations on startup and seeds initial data.

### Code Generation

ABP provides code generation CLI commands. Examples:

```bash
# Generate CRUD pages (UI specific - if Angular/Blazor exists)
abp generate

# Create new entity with full CRUD
abp generate-module --name ProductCategory
```

## Architecture Notes

### ABP Module System
All projects define module classes inheriting from `AbpModule`. Modules declare dependencies with `[DependsOn]` attribute. Key modules:
- `ProductManagementDomainModule`
- `ProductManagementApplicationModule`
- `ProductManagementHttpApiHostModule`
- `ProductManagementAuthServerModule`

### Multi-Tenancy
Multi-tenancy is enabled (`MultiTenancyConsts.IsEnabled = true`). The system supports tenant isolation with separate databases per tenant (configured in EF Core).

### Authentication & Authorization
- **OAuth2/OpenID Connect** via OpenIddict
- **JWT Bearer tokens** for API authentication
- **Permission system** defined in `ProductManagementPermissionDefinitionProvider.cs`
- Default admin user: `admin` (seeded by `ProductManagementTestDataSeedContributor`)

### Caching & Distributed Locking
Uses **Redis** for:
- Distributed cache (`AbpDistributedCacheOptions`)
- Distributed locks (`IDistributedLockProvider`)
- Data Protection keys (non-development)

### Localization
Extensive localization support in `ProductManagement.Domain.Shared/Localization/ProductManagement/` with 20+ languages. JSON resource files per culture.

### Entity Framework Core
- `ProductManagementDbContext` holds `DbSet<Product>`, `DbSet<Category>` and standard ABP tables.
- Migrations in `ProductManagement.EntityFrameworkCore/Migrations/`
- Repository pattern abstracted via ABP's `IRepository<T>` (standard CRUD operations provided out-of-box)

## Development Workflow

1. **Initial Setup**:
   - Ensure Redis is running (localhost:6379 default)
   - Configure connection strings in `appsettings.json` files
   - Generate/verify OpenIddict certificate (usually pre-generated): `dotnet dev-certs https -v -ep openiddict.pfx -p <password>`
   - Run `ProductManagement.DbMigrator` to create database
   - Optionally run `abp install-libs` to install frontend dependencies

2. **Start Development**:
   - Make changes to Domain entities (in `ProductManagement.Domain/Entities/`)
   - Create new migration: `dotnet ef migrations add YourMigration`
   - Run `ProductManagement.DbMigrator` to apply
   - Implement Application Services in `ProductManagement.Application/AppServices/`
   - Define DTOs and contracts in `ProductManagement.Application.Contracts/`
   - Add API controllers (if needed) in `ProductManagement.HttpApi.Host/Controllers/`

3. **Testing**:
   - Unit tests in Domain/Application test projects use ABP's in-memory test infrastructure
   - Integration/EF tests use real database (configured in test base)
   - Follow existing test patterns in `Samples/` directories

## Configuration Files

- `appsettings.json` & `appsettings.Development.json`: Main configuration (connection strings, Redis, CORS origins)
- `appsettings.secrets.json`: Sensitive data (gitignored) - local user secrets
- `launchSettings.json`: IIS Express and project profiles (ports, SSL)
- `common.props`: Shared MSBuild properties (C# version, warnings, ABP project type)

## Important Conventions

- **Entity IDs**: All entities use `Guid` IDs (inherits from `AuditedEntity<Guid>`)
- **DTO naming**: `CreateUpdate{Entity}Dto`, `{Entity}Dto`, `{Entity}Filter`
- **App Service interfaces**: Prefixed with `I` (`IProductAppService`)
- **Permissions**: Defined in `ProductManagementPermissionDefinitionProvider.cs` using format `ProductManagement.{Feature}.{Action}`
- **Settings**: Define in `ProductManagementSettingDefinitionProvider.cs`
- **Events**: Domain events defined as classes (e.g., `ProductCreatedEvent`)
- **AutoMapper**: Configured in `ProductManagementApplicationAutoMapperProfile.cs`

## Test Data Seeding

Initial data (admin user, roles, permissions) is seeded by:
- `ProductManagement.TestBase/ProductManagementTestDataSeedContributor.cs` (tests)
- `ProductManagement.Domain/OpenIddict/OpenIddictDataSeedContributor.cs` (production)

## External Dependencies

- **Database**: SQL Server or PostgreSQL (connection string in appsettings)
- **Redis**: Used for caching, distributed locking, and data protection keys
- **OpenIddict**: OAuth2/OpenID Connect server (certificate required)
- **ABP Framework**: All ABP packages managed via NuGet

## Troubleshooting

- **Port conflicts**: Check `Properties/launchSettings.json` for HTTPS/HTTP ports; modify if needed
- **Certificate errors**: Regenerate `openiddict.pfx` using command from README.md or delete and let ABP regenerate
- **Redis connection**: Ensure Redis server is running (`redis-server` or Windows service)
- **Database migrations**: If errors on startup, run `ProductManagement.DbMigrator` explicitly
- **Test failures**: Ensure test database is accessible (connection string in `TestBase` project's `appsettings.json`)

## Notes for Future Instances

- This is a standard ABP Framework solution; most "how-to" questions are answered by consulting the ABP documentation: https://docs.abp.io
- The business domain is simple: Products and Categories. New features typically follow DDD patterns: add entity to Domain, repository is automatic via `IRepository<T>`, add application service, define DTOs, add API controller if needed.
- **Never modify**: Generated code in `ProductManagement.EntityFrameworkCore/Migrations/` manually; instead create new migrations
- **Client libraries**: If adding npm packages, run `abp install-libs` to update mappings
- **Localization**: Add new language by copying `en.json` and translating values; place in `Domain.Shared/Localization/ProductManagement/`
- Always respect existing architectural boundaries; keep domain logic in Domain layer, not in Application or EntityFrameworkCore
