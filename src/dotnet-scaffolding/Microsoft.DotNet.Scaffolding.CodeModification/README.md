# Microsoft.DotNet.Scaffolding.CodeModification
Microsoft.DotNet.Scaffolding.CodeModification is a library designed to assist .NET developers in modifying and generating code as part of scaffolding operations in a dotnet-scaffold compatible scaffolder.
- CodeModificationStep : an implementation of Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep. To be used as part of the scaffolder builder.

Disk files and Roslyn documents share the same text-replacement behavior. `ReplaceSnippet` entries are matched as case-sensitive literal text. Multi-line matches tolerate LF, CRLF, and mixed newline boundaries without changing surrounding text or line endings. Missing target files and unmatched nonempty snippets are skipped. A null or empty snippet means insertion: prepend when `Prepend` is true, otherwise append, including to empty files. Replacement blocks are inserted literally; existing-block checks and case-insensitive `CheckBlock` guards prevent duplicates. Invalid HTML targets outside the project and file-access failures are reported as modification failures.

For example : 
```csharp
var builder = Host.CreateScaffoldBuilder();
var newScaffolder = builder.AddScaffolder("scaffolder");
newScaffolder.WithCategory("Custom")
    .WithDescription("Project for scaffolding.")
    .WithOption(projectOption)
    .WithStep<CodeModificationStep>(config =>
    {
        //context includes processed commandline information.
        var context = config.Context;
        //ScaffoldStep for initializing required properties
        var step = config.Step;
        step.Property = PropertyValue;
    });