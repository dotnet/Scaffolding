# Shared template inputs

Organize inputs as `<framework>\<template>`. Retain default template content and do not add scaffolder-specific models, services, or Identity code here.

Multiple scaffolders can copy these inputs into temporary directories and apply their own setup there. Expected outputs belong in scaffolder-specific baseline directories. Never run a scaffolder against the checked-in input itself.

The Blazor Web App inputs use per-page Blazor Server interactivity and no authentication. Pages use static SSR unless they explicitly specify `@rendermode InteractiveServer`. Interactive components run on the server; do not assume a request `HttpContext` is available during the circuit.
