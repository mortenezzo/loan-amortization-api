# Loan Amortization API

REST API for calculating French-method (fixed monthly payment) loan amortization schedules. Users can simulate loans, save results, and compare multiple simulations.

## Stack

- **Runtime**: .NET 10 / ASP.NET Core
- **Database**: PostgreSQL 16 (EF Core + Npgsql)
- **Auth**: JWT Bearer (BCrypt for password hashing)
- **CQRS**: MediatR + FluentValidation pipeline
- **Tests**: xUnit + FluentAssertions + Testcontainers

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (required for all setups)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (only for local development)

## Quick Start (Docker Compose)

```bash
# Copy and configure the JWT secret
cp .env.example .env
# Edit .env and set JWT_SECRET_KEY to a random string of at least 32 characters

# Build and start everything (migrations run automatically on startup)
docker-compose up -d

# API is now available at http://localhost:8080
# Swagger UI: not available in Production mode (see Local Development)
```

## Local Development

```bash
# Start only PostgreSQL
docker-compose up -d postgres

# Run the API with hot reload (Swagger UI available at http://localhost:5000/swagger)
dotnet run --project src/LoanAmortization.Api

# Apply migrations
dotnet ef database update \
  --project src/LoanAmortization.Infrastructure \
  --startup-project src/LoanAmortization.Api
```

## Running Tests

Docker must be running (Testcontainers spins up a real PostgreSQL container).

```bash
dotnet test
```

## Environment Variables

| Variable | Description | Default (dev) |
|----------|-------------|---------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Database=loan_db;...` |
| `Jwt__SecretKey` | Signing key (min 32 chars) | `dev-secret-key-minimum-32-chars-long!!` |
| `Jwt__Issuer` | JWT issuer | `loan-amortization-api` |
| `Jwt__Audience` | JWT audience | `loan-amortization-client` |
| `Jwt__ExpirationMinutes` | Token lifetime | `60` |

> In production, set `JWT_SECRET_KEY` in your `.env` file or deploy environment. Never use the dev default.

## API Reference

Full contract: [`docs/openapi.yaml`](docs/openapi.yaml)

### Auth

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/auth/register` | Register (email + password) |
| `POST` | `/api/v1/auth/login` | Login → returns JWT |

### Simulations

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/v1/simulate` | Optional | Calculate amortization schedule. Pass `"save": true` to persist (requires JWT) |
| `GET` | `/api/v1/simulations` | Required | List authenticated user's saved simulations |
| `GET` | `/api/v1/simulations/{id}` | Required | Get a saved simulation with recalculated schedule |
| `POST` | `/api/v1/simulations/compare` | Required | Compare multiple saved simulations by ID |

### System

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/health` | Health check |

### Example: Simulate a loan

```bash
curl -X POST http://localhost:8080/api/v1/simulate \
  -H "Content-Type: application/json" \
  -d '{"currency":"USD","principal":10000,"annual_rate":12,"term_months":12,"save":false}'
```

Response:
```json
{
  "simulation_id": null,
  "currency": "USD",
  "principal": 10000.00,
  "annual_rate": 12.0,
  "term_months": 12,
  "monthly_payment": 888.49,
  "total_payment": 10661.85,
  "total_interest": 661.85,
  "schedule": [
    { "month": 1, "payment": 888.49, "interest": 100.00, "capital": 788.49, "remaining_balance": 9211.51 },
    ...
  ]
}
```

## Validation Rules

| Field | Rule |
|-------|------|
| `currency` | `USD` or `PEN` only |
| `principal` | > 0 |
| `annual_rate` | > 0 and ≤ 100 |
| `term_months` | 1–360 |
| `password` | Minimum 8 characters |

## Project Structure

```
src/
├── LoanAmortization.Domain/          # Pure domain logic (AmortizationCalculator)
├── LoanAmortization.Application/     # CQRS handlers, interfaces, validators
├── LoanAmortization.Infrastructure/  # EF Core, repositories, JWT service
└── LoanAmortization.Api/             # Controllers, DTOs, middleware, Program.cs

tests/
├── LoanAmortization.Domain.Tests/    # Unit tests for AmortizationCalculator
└── LoanAmortization.Api.Tests/       # Integration tests (Testcontainers + WebApplicationFactory)

docs/
└── openapi.yaml                      # OpenAPI 3.1 contract (source of truth)
```
