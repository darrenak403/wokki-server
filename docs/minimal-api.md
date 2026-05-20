# Minimal API structure

## Root

[`PipelineExtensions.cs`](../src/Wokki.Api/Bootstrapping/PipelineExtensions.cs): `UseApplicationPipeline()` + `MapEndpoints()`.

## Module pattern (copy per feature)

File: `src/Wokki.Api/Apis/{Feature}/{Feature}Endpoints.cs`

1. `MapXxxApi()` — `MapGroup("/api/v1/...").MapXxxRoutes().WithTags(...).RequireRateLimiting("Fixed")`
2. `MapXxxRoutes()` — routes + `.WithName` + `.WithDescription` + `.Produces<ApiResponse<...>>` + auth
3. Handlers — `private static Task<IResult>`, `[FromBody]` / `[FromRoute]` / `[AsParameters]`, `[FromServices]`
4. Validation — `if (!request.ValidateRequest(validator, out var validationResult)) return validationResult!;`
5. Service — `return (await service....).ToHttpResult();`

Reference: [`Apis/Auth/AuthEndpoints.cs`](../src/Wokki.Api/Apis/Auth/AuthEndpoints.cs)

## Register

Add `app.MapXxxApi();` in `PipelineExtensions.MapEndpoints()`.
