# Microsoft.DotNet.Scaffolding.Roslyn
Microsoft.DotNet.Scaffolding.Roslyn is a library designed to assist .NET developers in need for CodeAnalysis related roslyn services.
- ICodeService : a service that manages loading, reading, and editing a Roslyn workspace
- MSBuildProjectService : a service that reads project capabilities, target frameworks, evaluated project references, and properties

`ICodeService.GetAllClassSymbolsAsync()` searches only the current project.
Use `GetAllClassSymbolsAsync(includeReferencedProjects: true)` for model
discovery across direct and transitive project references, including already-built class
libraries. `CodeService` uses an MSBuild workspace with source references enabled, so
the selected target frameworks, imports, properties, conditions, and
`ReferenceOutputAssembly` metadata are respected. The project graph supplies source declarations,
which are matched to types in the target compilation's resolved assembly references. This omits
libraries excluded by `DisableTransitiveProjectReferences` and consolidates framework variants
to the type identity actually resolved by the target project, without merging types from
different assemblies. Current-project classes are returned first. Package assemblies are not searched.
Reference-aware discovery returns only classes accessible from the target assembly,
including internal classes exposed to that assembly through `InternalsVisibleTo`.
Accessibility checks also account for containing types and nested class visibility.
Projects must be restored and load successfully; workspace failures are reported rather
than returning an incomplete reference graph.

The overload has a default interface implementation for compatibility with existing
`ICodeService` implementations: `false` delegates to parameterless discovery, while
`true` throws `NotSupportedException` unless the implementation overrides the overload.

Razor Views validation and its interactive model picker opt into referenced-model
discovery. The Views picker displays and submits fully qualified type names.
The `--model` option accepts qualified names such as `BookStore.Models.Book` or
`global::BookStore.Models.Book`, and short names such as `Book` when unambiguous.
Ambiguous names are rejected with qualified candidates instead of selecting the first
match. Inaccessible types are omitted from the picker and rejected during validation.
View templates retain the selected qualified identity, including containing types for nested models.
Generated view properties and getters are filtered for accessibility from the target assembly,
and primary-key validation uses that filtered property set.
EF scaffolders retain target-project-only discovery and short-name picker values; editing and registering
DbContexts from referenced projects is outside the scope of this Razor Views fix.