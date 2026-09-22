# Microsoft.DotNet.Scaffolding.Core
Microsoft.DotNet.Scaffolding.Core is a library designed to assist .NET developers in creating dotnet-scaffold compatible scaffolders in their own dotnet tools.

For example: 
- Install/reference Microsoft.DotNet.Scaffolding.Core
- Refer to Microsoft.DotNet.Scaffolding.Core.Builder.IScaffolderBuilder for all available builder helpers.
```csharp
var builder = Host.CreateScaffoldBuilder();
var newScaffolder = builder.AddScaffolder("scaffolder");
newScaffolder.WithCategory("Custom")
    .WithDescription("Project for scaffolding.")
    .WithOption(projectOption)
    .WithStep<ScaffoldStep>(config =>
    {
        //context includes processed commandline information.
        var context = config.Context;
        //ScaffoldStep for initializing required properties
        var step = config.Step;
        step.Property = PropertyValue;
    });
```

## Execution results

`IScaffolder.ExecuteAsync` returns `false` when a required step fails. Execution stops before that step's post-execution callback and any remaining steps. Skipped steps and failures in steps with `ContinueOnError` enabled do not fail the operation.

`IScaffoldRunner.RunAsync` returns the command exit code. Tool entry points should return this value so callers can detect parsing and scaffolding failures:

```csharp
var runner = builder.Build();
return await runner.RunAsync(args);
```

A failed required step reports its name and warns that the project or external resources may have been partially modified. A nonzero exit code does not imply that earlier changes were rolled back. Custom root handlers can return `Task<int>` to propagate their own exit codes.