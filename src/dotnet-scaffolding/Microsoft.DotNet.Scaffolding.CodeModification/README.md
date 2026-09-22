# Microsoft.DotNet.Scaffolding.CodeModification
Microsoft.DotNet.Scaffolding.CodeModification is a library designed to assist .NET developers in modifying and generating code as part of scaffolding operations in a dotnet-scaffold compatible scaffolder.
- CodeModificationStep : an implementation of Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep. To be used as part of the scaffolder builder.

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
```

## Conditional edits

Use `Options` on a file entry or an individual edit to require matching `CodeChangeOptions`. Names are case-insensitive, and every condition must match. Omitted or empty `Options` apply unconditionally. Prefix a name with `!` to require that option to be absent:

```json
"Options": [ "EfScenario", "!InteractiveServer" ]
```

This applies the edit only when `EfScenario` is present and `InteractiveServer` is absent. Negated names belong in the JSON conditions, not in the supplied `CodeChangeOptions`.