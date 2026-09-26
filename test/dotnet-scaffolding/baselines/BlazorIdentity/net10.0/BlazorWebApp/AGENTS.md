# BlazorWebApp

| Setting | Value |
|---------|-------|
| **Interactivity Mode** | Server |
| **Interactivity Scope** | Per-page |

## Rendering configuration
This project uses per-page Interactive Server with prerendering.
Created with `dotnet new blazor -int Server`.

Pages are static SSR by default. Only components that explicitly add `@rendermode InteractiveServer` become interactive.

## Adding new components
- Create new `.razor` files in `Components/Pages/` for routable pages or `Components/` for shared components.
- New pages are static SSR by default. Only add `@rendermode InteractiveServer` to components that need client-side behavior.
- Static pages can use standard HTML forms with `[SupplyParameterFromForm]`.

## Data access
- Components can inject services directly. No HTTP API layer is needed.

## Environment constraints
- Interactive components run on the server via SignalR. `HttpContext` is available in static components but not during an interactive circuit.
- Static pages can access `HttpContext` via `[CascadingParameter]`.
- Use `IJSRuntime` for browser APIs.

## Authentication
ASP.NET Core Identity is configured. Identity pages under `Components/Account/` always use static SSR; do not add `@rendermode` to them.

## Baseline contract
This application is expected scaffolder output. Retain the default template pages. Follow the baseline README when updating it; do not change expected output merely to accept a failing test.
