# CLAUDE.md — Loan Amortization API

Contexto del proyecto para sesiones de AI. Leer antes de cualquier tarea.

---

## ¿Qué es este proyecto?

API REST en **C# / ASP.NET Core (.NET 10)** que simula el cuadro de amortización de préstamos bancarios bajo el **método francés** (cuota fija mensual). Los usuarios se registran, obtienen un JWT, simulan préstamos (opcionalmente guardándolos) y pueden comparar múltiples simulaciones guardadas.

Ver `PLAN.md` para el roadmap de implementación completo.

---

## Arquitectura — Clean Architecture + CQRS (MediatR)

**Regla de dependencias**: las flechas apuntan siempre hacia adentro.
`Api → Application → Domain` | `Infrastructure → Domain`
Infrastructure nunca depende de Api.

### Proyectos

```
src/
├── LoanAmortization.Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   └── Simulation.cs
│   ├── ValueObjects/
│   │   └── InstallmentRow.cs
│   └── Services/
│       └── AmortizationCalculator.cs     ← lógica pura, cero dependencias externas
│
├── LoanAmortization.Application/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   └── ValidationBehavior.cs     ← MediatR pipeline (FluentValidation)
│   │   └── Interfaces/
│   │       ├── ISimulationRepository.cs
│   │       ├── IUserRepository.cs
│   │       └── ITokenService.cs
│   ├── Auth/
│   │   ├── Register.cs                   ← RegisterCommand + Handler + Result
│   │   └── Login.cs                      ← LoginCommand + Handler + Result
│   └── Simulations/
│       ├── Simulate.cs                   ← SimulateCommand + Handler + Result
│       ├── ListSimulations.cs            ← ListSimulationsQuery + Handler + Result
│       ├── GetSimulation.cs              ← GetSimulationQuery + Handler + Result
│       └── Compare.cs                    ← CompareCommand + Handler + Result
│
├── LoanAmortization.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/               ← EF Fluent API (nunca DataAnnotations en entidades)
│   │   │   ├── UserConfiguration.cs
│   │   │   └── SimulationConfiguration.cs
│   │   └── Migrations/
│   ├── Repositories/
│   │   ├── EfSimulationRepository.cs     ← implementa ISimulationRepository
│   │   └── EfUserRepository.cs           ← implementa IUserRepository
│   └── Auth/
│       └── JwtTokenService.cs            ← implementa ITokenService
│
└── LoanAmortization.Api/
    ├── Controllers/
    │   ├── AuthController.cs
    │   ├── SimulationsController.cs
    │   └── HealthController.cs
    ├── DTOs/                             ← modelos HTTP (request/response), separados de Application
    ├── Middleware/
    │   └── ExceptionMiddleware.cs        ← ProblemDetails RFC 9457
    └── Program.cs

tests/
├── LoanAmortization.Domain.Tests/        ← xUnit unit tests para AmortizationCalculator
└── LoanAmortization.Api.Tests/           ← integration tests con WebApplicationFactory
```

### Convención de feature files (CQRS)

Cada operación vive en un solo archivo con tres tipos internos:

```csharp
// Application/Simulations/Simulate.cs
public record SimulateCommand(...) : IRequest<SimulateResult>;

public class SimulateCommandValidator : AbstractValidator<SimulateCommand> { ... }

public class SimulateHandler : IRequestHandler<SimulateCommand, SimulateResult>
{
    // inyecta ISimulationRepository, AmortizationCalculator, etc.
}

public record SimulateResult(...);
```

### Flujo de una request

```
HTTP Request
  → Controller (mapea DTO → Command/Query)
  → MediatR.Send()
    → ValidationBehavior (FluentValidation, lanza si falla)
    → Handler (lógica de aplicación, usa repositorios y domain services)
      → AmortizationCalculator (domain puro)
      → ISimulationRepository (persistence)
  → Controller (mapea Result → DTO de respuesta HTTP)
HTTP Response
```

La especificación vive en `docs/openapi.yaml`.

---

## Metodología — API-First Design + SDD

Este proyecto aplica **API-First Design** (también conocido como Design-First o Contract-First): el contrato OpenAPI (`docs/openapi.yaml`) se escribe **antes** de tocar código C#. La implementación debe cumplir el contrato, no al revés.

Esto se combina con **SDD (Specification-Driven Development)** a nivel de dominio: los tests xUnit del calculador de amortización se escriben antes de la implementación (TDD clásico).

**Orden obligatorio: spec → tests → implementación. Nunca al revés.** Nunca modificar el comportamiento sin actualizar primero el `openapi.yaml` y los tests.

---

## Reglas del proyecto

### Cálculos financieros
- Usar `decimal` (nunca `double` ni `float`) para todo cálculo de dinero.
- Redondear a **2 decimales** únicamente en el response. Internamente mantener precisión máxima.
- Monedas soportadas: `USD` y `PEN`. Sin soporte para otras monedas.

### Fórmula de amortización (Método Francés)
```
r   = annual_rate / 100 / 12
PMT = principal × r / (1 − (1 + r)^(−term_months))

Por cada mes i:
  interest_i  = balance_{i-1} × r
  capital_i   = PMT − interest_i
  balance_i   = balance_{i-1} − capital_i
```

### Persistencia
- La tabla de cuotas (`InstallmentRow[]`) **nunca se guarda en BD** — se recalcula desde los parámetros de la simulación.
- La tabla `simulations` guarda únicamente: `principal`, `annual_rate`, `term_months`, `currency`, `user_id`, `created_at`.

### Auth
- JWT Bearer. Los tokens los genera esta misma API.
- Contraseñas hasheadas con BCrypt.
- El endpoint `POST /simulate` acepta requests sin auth (cálculo inline sin guardar). Para guardar (`save: true`) sí requiere JWT.

---

## Convenciones de código

- **Naming C#**: PascalCase para clases/métodos, camelCase para variables locales, snake_case para JSON fields en la API.
- **Naming URLs**: Rutas siempre en **kebab-case** — `/api/v1/simulations/compare`, `/api/v1/auth/register`. Nunca PascalCase ni camelCase en paths (~~`/api/v1/GetSimulations`~~).
- **Carpetas**: `Controllers/`, `DTOs/`, `Services/`, `Repositories/`, `Entities/`.
- **No comments**: Solo cuando el WHY no es obvio (e.g., workaround de precisión decimal).
- **No mocks de DB en integration tests**: Los tests de integración usan una instancia real de PostgreSQL (ver docker-compose).
- **Manejo de errores**: ProblemDetails RFC 9457 vía middleware global. No try/catch en controllers.
- **Validación**: FluentValidation o DataAnnotations en los DTOs. Nunca validar en el Domain.

---

## Stack de dependencias clave

| Paquete | Uso |
|---------|-----|
| `MediatR` | CQRS — dispatching de Commands y Queries |
| `FluentValidation.AspNetCore` | Validación en pipeline MediatR |
| `Microsoft.EntityFrameworkCore` | ORM |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Provider PostgreSQL |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Validación JWT |
| `System.IdentityModel.Tokens.Jwt` | Generación JWT |
| `BCrypt.Net-Next` | Hash de contraseñas |
| `xunit` + `FluentAssertions` | Tests |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration tests |
| `Swashbuckle.AspNetCore` | Swagger UI (solo dev) |

---

## Variables de entorno / configuración

```json
// appsettings.json (estructura esperada)
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=loan_db;Username=postgres;Password=secret"
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "loan-amortization-api",
    "Audience": "loan-amortization-client",
    "ExpirationMinutes": 60
  }
}
```

En producción/Docker se sobreescriben con variables de entorno (ASP.NET Core convention: `__` como separador).

---

## Cómo levantar el proyecto

```bash
# Levantar PostgreSQL + API
docker-compose up -d

# Solo desarrollo local (PostgreSQL en Docker, API con hot reload)
docker-compose up -d postgres
dotnet run --project src/LoanAmortization.Api

# Correr todos los tests
dotnet test

# Aplicar migrations
dotnet ef database update --project src/LoanAmortization.Infrastructure --startup-project src/LoanAmortization.Api
```

---

## Estructura de la BD

```sql
users (id UUID PK, email, password_hash, created_at)
simulations (id UUID PK, user_id FK → users.id, currency CHAR(3), principal NUMERIC(18,2), annual_rate NUMERIC(7,4), term_months INT, created_at)
```

---

## Lo que NO está en scope (fase actual)

- Amortización alemana (capital fijo)
- Otras monedas fuera de USD/PEN
- Pagos anticipados o cambios de cuota
- Roles/permisos más allá de usuario autenticado
- Rate limiting
- Notificaciones
