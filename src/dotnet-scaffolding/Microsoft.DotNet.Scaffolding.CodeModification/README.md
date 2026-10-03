# Microsoft.DotNet.Scaffolding.CodeModification
Microsoft.DotNet.Scaffolding.CodeModification is a library designed to assist .NET developers in modifying and generating code as part of scaffolding operations in a dotnet-scaffold compatible scaffolder.
- CodeModificationStep : an implementation of Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep. To be used as part of the scaffolder builder.

`ReplaceSnippet` entries are matched as literal text. Multi-line matches tolerate LF, CRLF, and mixed newline boundaries without changing surrounding text or line endings. Replacement blocks are inserted literally, and existing `CheckBlock` guards still control whether a replacement is applied.

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