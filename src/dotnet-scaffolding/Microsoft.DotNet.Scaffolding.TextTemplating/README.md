# Microsoft.DotNet.Scaffolding.TextTemplating
Microsoft.DotNet.Scaffolding.TextTemplating is a library designed to assist .NET developers in generating templated files as part of scaffolding operations in a dotnet-scaffold compatible scaffolder.
- TextTemplatingStep : an implementation of Microsoft.DotNet.Scaffolding.Core.Steps.ScaffoldStep. To be used as part of the scaffolder builder.

For example : 
```csharp
var builder = Host.CreateScaffoldBuilder();
var newScaffolder = builder.AddScaffolder("scaffolder");
newScaffolder.WithCategory("Custom")
    .WithDescription("Project for scaffolding!")
    .WithOption(projectOption)
    .WithStep<TextTemplatingStep>(config =>
    {
        //context includes processed commandline information.
        var context = config.Context;
        //ScaffoldStep for initializing required properties
        var step = config.Step;
        step.Property = PropertyValue;
    });