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

## File-backed code snippets

Use `FileBlock` instead of `Block` or `MultiLineBlock` to load a larger snippet from a plain text file:

```json
{
  "CheckBlock": "<AuthorizeView>",
  "ReplaceSnippet": [ "</nav>" ],
  "FileBlock": "Blocks\\IdentityNavMenu.razor"
}
```

Paths are relative to the JSON configuration file, not the project being scaffolded. Multiple configurations can reference the same fragment. Include the fragment files in the scaffolder's build output and package, preserving those relative paths.

`FileBlock` is supported for method `CodeChanges` and file `Replacements` in configurations supplied through `CodeModifierConfigPath`. Inline configurations supplied through `CodeModifierConfigJsonText` cannot use `FileBlock`. A snippet cannot combine `FileBlock` with `Block` or `MultiLineBlock`.

The file's lines are joined with the platform newline, just like `MultiLineBlock`. Leading whitespace and blank lines are preserved; the final line terminator is not part of the block. Include a final blank line if the inserted text needs a trailing newline. Replacement fragments must include any matched text that should remain, such as the closing `</nav>` in this example.

Loaded fragments use the existing placeholder substitution, options, and insertion behavior. Missing or invalid file references are reported before the configuration modifies the project.