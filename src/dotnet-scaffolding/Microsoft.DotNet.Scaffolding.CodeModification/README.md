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

HTML recipes use Roslyn additional documents when available. Otherwise, `Replacements` can edit HTML files on disk within the project, including standalone Blazor WebAssembly's `wwwroot\index.html`. File and replacement `Options` and `CodeModifierProperties` substitutions apply to both paths. Use `CheckBlock` to prevent duplicate insertions on reruns:

```json
{
  "Files": [
    {
      "FileName": "wwwroot\\index.html",
      "Replacements": [
        {
          "ReplaceSnippet": ["</head>"],
          "MultiLineBlock": [
            "    <link href=\"example.css\" rel=\"stylesheet\" />",
            "</head>"
          ],
          "CheckBlock": "example.css"
        }
      ]
    }
  ]
}
```

For on-disk HTML edits, a missing target, an unmatched required replacement snippet, or a file read/write error is logged and causes the step to return `false`. Replacements already present (including those recognized by `CheckBlock`) and edits excluded by options are successful no-ops.