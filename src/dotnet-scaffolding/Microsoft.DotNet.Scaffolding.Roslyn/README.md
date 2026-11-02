# Microsoft.DotNet.Scaffolding.Roslyn
Microsoft.DotNet.Scaffolding.Roslyn is a library designed to assist .NET developers in need for CodeAnalysis related roslyn services.
- ICodeService : a service that manages loading, reading, and editing a Roslyn workspace
- MSBuildProjectService : a service that reads project capabilities, target frameworks, evaluated project references, and properties

`ICodeService.GetAllClassSymbolsAsync()` searches only the current project.
Use `GetAllClassSymbolsAsync(includeReferencedProjects: true)` for model and DbContext
discovery across direct and transitive project references, including already-built class
libraries. References are evaluated by MSBuild, so imports, properties, and conditions
are respected. Current-project classes are returned first, and each referenced project
is searched once. Package assemblies are not searched.