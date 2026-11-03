# Microsoft.DotNet.Scaffolding.Roslyn
Microsoft.DotNet.Scaffolding.Roslyn is a library designed to assist .NET developers in need for CodeAnalysis related roslyn services.
- ICodeService : a service that manages loading, reading, and editing a Roslyn workspace
- MSBuildProjectService : a service that reads project capabilities, target frameworks, evaluated project references, and properties

`ICodeService.GetAllClassSymbolsAsync()` searches only the current project.
Use `GetAllClassSymbolsAsync(includeReferencedProjects: true)` for model
discovery across direct and transitive project references, including already-built class
libraries. `CodeService` uses an MSBuild workspace with source references enabled, so
the selected target frameworks, imports, properties, conditions, and
`ReferenceOutputAssembly` metadata are respected. Current-project classes are returned
first, and each resolved project is searched once. Package assemblies are not searched.
Projects must be restored and load successfully; workspace failures are reported rather
than returning an incomplete reference graph.

The overload has a default interface implementation for compatibility with existing
`ICodeService` implementations: `false` delegates to parameterless discovery, while
`true` throws `NotSupportedException` unless the implementation overrides the overload.

Razor Views validation and its interactive model picker opt into referenced-model
discovery. EF scaffolders retain target-project-only discovery; editing and registering
DbContexts from referenced projects is outside the scope of this Razor Views fix.