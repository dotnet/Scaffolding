# Microsoft.DotNet.Scaffolding.Core
Microsoft.DotNet.Scaffolding.Core is a library designed to assist .NET developers in creating dotnet-scaffold compatible scaffolders in their own dotnet tools.

Console logging displays errors and critical messages in bright red and warnings in bright yellow. Other log levels retain the default terminal color. Redirected output contains no added ANSI color codes.
Color sequences use separate brightness and foreground attributes for compatibility with the console logger's Windows fallback parser.

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