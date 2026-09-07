# PLAN — Loan Amortization API

## Contexto

Se necesita una API REST que simule el cuadro de amortización de un préstamo bancario bajo el **método francés** (cuota fija mensual). El usuario provee tres parámetros (monto, tasa anual y plazo en meses) y recibe la tabla completa mes a mes: cuota, interés, capital amortizado y saldo restante.

El proyecto aplica **API-First Design** (Design-First / Contract-First): el `openapi.yaml` se escribe antes de tocar código. Combinado con **SDD**: los tests del dominio se escriben antes que la implementación. Orden: spec → tests → código. Nunca al revés.

---

## Stack técnico

| Capa | Tecnología |
|------|-----------|
| Runtime | .NET 10 |
| Framework | ASP.NET Core (Minimal APIs o Controllers) |
| ORM | EF Core 10 + Npgsql |
| Base de datos | PostgreSQL 16 |
| Auth | JWT Bearer (System.IdentityModel.Tokens.Jwt) |
| CQRS | MediatR |
| Validación | FluentValidation (pipeline behavior) |
| Tests | xUnit + FluentAssertions |
| Contenedores | Docker + docker-compose |
| Spec | OpenAPI 3.1 (openapi.yaml) |

---

## Arquitectura — Clean Architecture + CQRS (MediatR)

**Regla de dependencias**: `Api → Application → Domain` | `Infrastructure → Domain`

```
src/
├── LoanAmortization.Domain/
│   ├── Entities/                         # User.cs, Simulation.cs
│   ├── ValueObjects/                     # InstallmentRow.cs
│   └── Services/
│       └── AmortizationCalculator.cs     # Lógica pura, sin dependencias externas
│
├── LoanAmortization.Application/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   └── ValidationBehavior.cs     # MediatR pipeline (FluentValidation)
│   │   └── Interfaces/
│   │       ├── ISimulationRepository.cs
│   │       ├── IUserRepository.cs
│   │       └── ITokenService.cs
│   ├── Auth/
│   │   ├── Register.cs                   # Command + Handler + Result
│   │   └── Login.cs
│   └── Simulations/
│       ├── Simulate.cs
│       ├── ListSimulations.cs
│       ├── GetSimulation.cs
│       └── Compare.cs
│
├── LoanAmortization.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/               # EF Fluent API
│   │   └── Migrations/
│   ├── Repositories/
│   │   ├── EfSimulationRepository.cs
│   │   └── EfUserRepository.cs
│   └── Auth/
│       └── JwtTokenService.cs
│
└── LoanAmortization.Api/
    ├── Controllers/
    │   ├── AuthController.cs
    │   ├── SimulationsController.cs
    │   └── HealthController.cs
    ├── DTOs/
    ├── Middleware/
    │   └── ExceptionMiddleware.cs        # ProblemDetails RFC 9457
    └── Program.cs

tests/
├── LoanAmortization.Domain.Tests/
└── LoanAmortization.Api.Tests/

docs/
└── openapi.yaml                          # Fuente de verdad (API-First)

docker-compose.yml
docker-compose.override.yml
Dockerfile
```

### Flujo de una request

```
HTTP → Controller → MediatR.Send(Command/Query)
  → ValidationBehavior (FluentValidation)
  → Handler → AmortizationCalculator + ISimulationRepository
→ Controller → HTTP Response
```

---

## Dominio

### Fórmula — Método Francés

```
r   = tasa_anual / 100 / 12
PMT = P × r / (1 − (1 + r)^(−n))

Por cada mes i:
  interest_i  = balance_{i-1} × r
  capital_i   = PMT − interest_i
  balance_i   = balance_{i-1} − capital_i
```

- Todos los cálculos internos usan `decimal` (28 dígitos de precisión).
- El output se redondea a **2 decimales**.
- Monedas soportadas: **USD** y **PEN**.

### Entidades

```csharp
// Domain
record InstallmentRow(int Month, decimal Payment, decimal Interest, decimal Capital, decimal RemainingBalance);

class Simulation          // id, user_id, currency, principal, annual_rate, term_months, created_at
class User                // id, email, password_hash, created_at
```

La tabla de cuotas (`InstallmentRow[]`) **no se persiste** — se recalcula siempre desde los parámetros de la simulación.

---

## Endpoints

### Auth
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | /api/v1/auth/register | Registrar usuario (email + password) |
| POST | /api/v1/auth/login | Login → devuelve JWT |

### Simulaciones
| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | /api/v1/simulate | Opcional | Calcular tabla. Si `save: true` → persiste (requiere JWT) |
| GET | /api/v1/simulations | Requerida | Listar simulaciones del usuario autenticado |
| GET | /api/v1/simulations/{id} | Requerida | Obtener simulación guardada (con tabla recalculada) |

### Comparación
| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | /api/v1/simulations/compare | Requerida | Comparar N simulaciones guardadas del usuario por IDs |

### Sistema
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | /api/v1/health | Estado de la API |

---

## Contratos de ejemplo

### POST /api/v1/simulate — Request
```json
{
  "currency": "USD",
  "principal": 10000.00,
  "annual_rate": 12.0,
  "term_months": 12,
  "save": false
}
```

### POST /api/v1/simulate — Response
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
    { "month": 2, "payment": 888.49, "interest": 92.12, "capital": 796.37, "remaining_balance": 8415.14 },
    "..."
  ]
}
```

### POST /api/v1/simulations/compare — Request
```json
{ "simulation_ids": ["uuid-1", "uuid-2", "uuid-3"] }
```

### POST /api/v1/simulations/compare — Response
```json
{
  "simulations": [
    {
      "simulation_id": "uuid-1",
      "currency": "USD",
      "principal": 10000.00,
      "annual_rate": 12.0,
      "term_months": 12,
      "monthly_payment": 888.49,
      "total_payment": 10661.85,
      "total_interest": 661.85
    },
    "..."
  ]
}
```

---

## Fases de implementación (SDD)

### Fase 0 — Spec (OpenAPI primero)
- [ ] Escribir `docs/openapi.yaml` con todos los endpoints, modelos, ejemplos y códigos de error
- [ ] Revisar que el contrato sea completo y coherente antes de tocar código

### Fase 1 — Bootstrap del proyecto
- [ ] Crear solución .NET 9 con los 4 proyectos (`Api`, `Application`, `Domain`, `Infrastructure`)
- [ ] Crear proyectos de test (`Domain.Tests`, `Api.Tests`)
- [ ] Configurar `docker-compose.yml` con PostgreSQL
- [ ] Configurar `Dockerfile` para la API

### Fase 2 — Dominio (TDD: tests primero)
- [ ] Escribir tests unitarios para `AmortizationCalculator` (RED)
  - Cuota mensual correcta
  - Suma de capitales == principal
  - Balance final == 0
  - Casos borde: tasa 0%, plazo 1 mes
- [ ] Implementar `AmortizationCalculator` (GREEN)
- [ ] Refactorizar si aplica (REFACTOR)

### Fase 3 — Infraestructura base
- [ ] Configurar `AppDbContext` con EF Core 9 + Npgsql
- [ ] Definir entidades `User` y `Simulation` con fluent config
- [ ] Crear migrations iniciales
- [ ] Implementar `UserRepository` e `SimulationRepository`

### Fase 4 — Auth
- [ ] Implementar `POST /auth/register` con hash de contraseña (BCrypt)
- [ ] Implementar `POST /auth/login` con generación de JWT
- [ ] Configurar middleware de validación JWT en ASP.NET Core

### Fase 5 — Endpoints principales
- [ ] `POST /simulate` (sin persistencia primero, luego con `save: true`)
- [ ] `GET /simulations` (lista del usuario autenticado)
- [ ] `GET /simulations/{id}`
- [ ] `POST /simulations/compare`
- [ ] `GET /health`

### Fase 6 — Tests de integración
- [ ] Setup de `WebApplicationFactory` con DB de test
- [ ] Tests de flujo completo: register → login → simulate → save → compare

### Fase 7 — Pulido
- [ ] Manejo global de errores (middleware ProblemDetails)
- [ ] Validación de inputs (FluentValidation o DataAnnotations)
- [ ] Swagger UI configurado en desarrollo
- [ ] README.md actualizado con instrucciones de setup

---

## Schema de base de datos

```sql
CREATE TABLE users (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email        VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE simulations (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  currency     CHAR(3) NOT NULL,              -- 'USD' | 'PEN'
  principal    NUMERIC(18, 2) NOT NULL,
  annual_rate  NUMERIC(7, 4) NOT NULL,        -- e.g. 12.0000 = 12%
  term_months  INT NOT NULL,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Validaciones de negocio

| Campo | Regla |
|-------|-------|
| `currency` | Debe ser `USD` o `PEN` |
| `principal` | > 0 |
| `annual_rate` | > 0 y ≤ 100 |
| `term_months` | >= 1 y ≤ 360 |
| `password` | Mínimo 8 caracteres |

---

## Verificación final

1. `docker-compose up -d` — levanta PostgreSQL + API
2. `POST /api/v1/auth/register` → token JWT
3. `POST /api/v1/simulate` con `save: true` → tabla + simulation_id
4. `GET /api/v1/simulations/{id}` → misma tabla recalculada
5. Repetir con una segunda simulación distinta
6. `POST /api/v1/simulations/compare` con los 2 IDs → resumen comparativo
7. `dotnet test` — todos los tests en verde
