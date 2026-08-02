# AWAKEN Project Base Scaffold — Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement task-by-task.

**Goal:** Scaffold the full AWAKEN monorepo — Flutter mobile app + ASP.NET Core backend + docker-compose + GitHub Actions CI.

**Architecture:** Feature-first Flutter + Clean Architecture lite on mobile; Modular Monolith + Clean Architecture + CQRS (MediatR) on backend. Mono-repo structure with `apps/mobile/`, `backend/`, `infra/`, `.github/`.

**Tech Stack:** Flutter/Dart (Riverpod, go_router, Dio, Drift), ASP.NET Core .NET 10 (MediatR, EF Core, FluentValidation, Serilog), PostgreSQL, Redis, Docker.

---

## Phase 1 — Backend (.NET Solution)

### Task 1: Create .NET solution and projects

- [ ] Run from repo root:
```bash
cd backend
dotnet new sln -n Awaken
dotnet new webapi -n Awaken.Api --framework net10.0 -o src/Awaken.Api
dotnet new classlib -n Awaken.Application --framework net10.0 -o src/Awaken.Application
dotnet new classlib -n Awaken.Domain --framework net10.0 -o src/Awaken.Domain
dotnet new classlib -n Awaken.Infrastructure --framework net10.0 -o src/Awaken.Infrastructure
dotnet new classlib -n Awaken.Contracts --framework net10.0 -o src/Awaken.Contracts
dotnet new classlib -n Awaken.Shared --framework net10.0 -o src/Awaken.Shared
dotnet new xunit -n Awaken.UnitTests --framework net10.0 -o tests/Awaken.UnitTests
dotnet new xunit -n Awaken.IntegrationTests --framework net10.0 -o tests/Awaken.IntegrationTests
dotnet new xunit -n Awaken.ArchitectureTests --framework net10.0 -o tests/Awaken.ArchitectureTests
```

- [ ] Add projects to solution:
```bash
dotnet sln add src/Awaken.Api/Awaken.Api.csproj
dotnet sln add src/Awaken.Application/Awaken.Application.csproj
dotnet sln add src/Awaken.Domain/Awaken.Domain.csproj
dotnet sln add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj
dotnet sln add src/Awaken.Contracts/Awaken.Contracts.csproj
dotnet sln add src/Awaken.Shared/Awaken.Shared.csproj
dotnet sln add tests/Awaken.UnitTests/Awaken.UnitTests.csproj
dotnet sln add tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj
dotnet sln add tests/Awaken.ArchitectureTests/Awaken.ArchitectureTests.csproj
```

- [ ] Add project references:
```bash
dotnet add src/Awaken.Api/Awaken.Api.csproj reference src/Awaken.Application/Awaken.Application.csproj
dotnet add src/Awaken.Api/Awaken.Api.csproj reference src/Awaken.Infrastructure/Awaken.Infrastructure.csproj
dotnet add src/Awaken.Api/Awaken.Api.csproj reference src/Awaken.Contracts/Awaken.Contracts.csproj
dotnet add src/Awaken.Application/Awaken.Application.csproj reference src/Awaken.Domain/Awaken.Domain.csproj
dotnet add src/Awaken.Application/Awaken.Application.csproj reference src/Awaken.Contracts/Awaken.Contracts.csproj
dotnet add src/Awaken.Application/Awaken.Application.csproj reference src/Awaken.Shared/Awaken.Shared.csproj
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj reference src/Awaken.Application/Awaken.Application.csproj
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj reference src/Awaken.Domain/Awaken.Domain.csproj
dotnet add src/Awaken.Domain/Awaken.Domain.csproj reference src/Awaken.Shared/Awaken.Shared.csproj
dotnet add tests/Awaken.UnitTests/Awaken.UnitTests.csproj reference src/Awaken.Application/Awaken.Application.csproj
dotnet add tests/Awaken.UnitTests/Awaken.UnitTests.csproj reference src/Awaken.Domain/Awaken.Domain.csproj
dotnet add tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj reference src/Awaken.Api/Awaken.Api.csproj
dotnet add tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj reference src/Awaken.Infrastructure/Awaken.Infrastructure.csproj
dotnet add tests/Awaken.ArchitectureTests/Awaken.ArchitectureTests.csproj reference src/Awaken.Api/Awaken.Api.csproj
```

### Task 2: Add NuGet packages

- [ ] Application layer:
```bash
dotnet add src/Awaken.Application/Awaken.Application.csproj package MediatR --version 12.*
dotnet add src/Awaken.Application/Awaken.Application.csproj package FluentValidation --version 11.*
dotnet add src/Awaken.Application/Awaken.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 11.*
dotnet add src/Awaken.Application/Awaken.Application.csproj package Microsoft.Extensions.Logging.Abstractions
```

- [ ] Infrastructure layer:
```bash
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 10.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 10.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package StackExchange.Redis --version 2.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package Serilog.AspNetCore --version 8.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package Serilog.Sinks.Console --version 5.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package OpenTelemetry.Extensions.Hosting --version 1.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package OpenTelemetry.Instrumentation.AspNetCore --version 1.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package OpenTelemetry.Exporter.Console --version 1.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package FirebaseAdmin --version 3.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package OpenAI --version 2.*
dotnet add src/Awaken.Infrastructure/Awaken.Infrastructure.csproj package AWSSDK.S3 --version 3.*
```

- [ ] API layer:
```bash
dotnet add src/Awaken.Api/Awaken.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.*
dotnet add src/Awaken.Api/Awaken.Api.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.*
dotnet add src/Awaken.Api/Awaken.Api.csproj package Swashbuckle.AspNetCore --version 7.*
dotnet add src/Awaken.Api/Awaken.Api.csproj package Asp.Versioning.Mvc --version 8.*
dotnet add src/Awaken.Api/Awaken.Api.csproj package Asp.Versioning.Mvc.ApiExplorer --version 8.*
dotnet add src/Awaken.Api/Awaken.Api.csproj package Microsoft.AspNetCore.Diagnostics.HealthChecks
dotnet add src/Awaken.Api/Awaken.Api.csproj package AspNetCore.HealthChecks.NpgSql --version 9.*
dotnet add src/Awaken.Api/Awaken.Api.csproj package AspNetCore.HealthChecks.Redis --version 9.*
```

- [ ] Test projects:
```bash
dotnet add tests/Awaken.UnitTests/Awaken.UnitTests.csproj package FluentAssertions --version 6.*
dotnet add tests/Awaken.UnitTests/Awaken.UnitTests.csproj package Moq --version 4.*
dotnet add tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj package Testcontainers.PostgreSql --version 3.*
dotnet add tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj package Testcontainers.Redis --version 3.*
dotnet add tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj package FluentAssertions --version 6.*
dotnet add tests/Awaken.ArchitectureTests/Awaken.ArchitectureTests.csproj package NetArchTest.Rules --version 1.*
dotnet add tests/Awaken.ArchitectureTests/Awaken.ArchitectureTests.csproj package FluentAssertions --version 6.*
```

### Task 3: Backend folder structure + key files

Create these directories and placeholder files manually (see file list below).

---

## Phase 2 — Docker Compose

### Task 4: docker-compose.yml

Create `docker-compose.yml` at repo root.

---

## Phase 3 — Flutter App

### Task 5: Flutter pubspec.yaml and folder structure

Create `apps/mobile/` manually since Flutter CLI not in PATH.

---

## Phase 4 — GitHub Actions

### Task 6: CI workflows

Create `.github/workflows/backend.yml` and `.github/workflows/mobile.yml`.

---
