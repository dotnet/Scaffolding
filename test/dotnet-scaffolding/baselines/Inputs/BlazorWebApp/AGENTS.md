# Shared Blazor Web App inputs

These are default Blazor Web Apps with per-page Blazor Server interactivity and no authentication. Retain the template pages and do not add scaffolder-specific models, services, or Identity code here.

Multiple scaffolders can copy these inputs into temporary directories and apply their own setup there. Expected outputs belong in scaffolder-specific baseline directories. Never run a scaffolder against the checked-in input itself.

Pages use static SSR unless they explicitly specify `@rendermode InteractiveServer`. Interactive components run on the server; do not assume a request `HttpContext` is available during the circuit.
