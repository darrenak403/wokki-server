# Architecture

Business context: [brd.md](./brd.md) · Rules: [business-rules.md](./business-rules.md) · Flows: [process-flows.md](./process-flows.md)

## Layers

| Project                | Responsibility                                                            |
| ---------------------- | ------------------------------------------------------------------------- |
| `Wokki.Common`         | `ApiResponse<T>`, `AppMessage`, `ToHttpResult()`                          |
| `Wokki.Domain`         | Entities, Constants (`RoleConstants`, …), `IUnitOfWork`, `IXxxRepository` |
| `Wokki.Application`    | Services, DTOs, FluentValidation, ports                                   |
| `Wokki.Infrastructure` | EF Core, JWT, cache, adapters                                             |
| `Wokki.Api`            | HTTP, middleware, DI root                                                 |

## Response pattern

Services return `ApiResponse<T>`:

- `ApiResponse<T>.SuccessResponse(data, message)`
- `ApiResponse<T>.FailureResponse(message, errors?)`
- `ApiResponse<T>.SuccessPagedResponse(...)`

Endpoints: `return (await service.Method(...)).ToHttpResult();`

## Add a new feature

1. Domain: entity + `IXxxRepository` + property on `IUnitOfWork`
2. Infrastructure: repository impl, EF configuration
3. `AppMessages`: add message codes with HTTP status
4. Application: DTOs, validator, `IXxxService`
5. Api: map endpoints + authorization policy
6. Migration: `dotnet ef migrations add ...`

## Authorization

- Policies: `Authenticated`, `Admin` (see `PolicyNames`)
- Resource-based: extend `IPermissionService` and call from services before mutate/read

## Multi-tenant (deferred)

- `ITenantContext`, global query filter TODO in `AppDbContext` (`User.TenantId` retained as hook)

## Cache (deferred Redis)

- `ICacheService` — memory implementation; swap to Redis adapter later
- Pattern: cache-aside + explicit invalidation on writes
