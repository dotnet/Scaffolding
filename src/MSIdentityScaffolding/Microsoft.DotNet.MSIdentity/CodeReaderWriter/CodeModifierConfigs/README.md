ach project type/scenario should have a json config file for itself.

class CodeModifierConfig
{
    public string? Identifier { get; set; } --> identifier for project type/scenario.
    public File[]? Files { get; set; } --> All the files that need editing in the project.
}

class File
{
    public Dictionary<string, Method>? Methods { get; set; } --> all the methods in the file that need editing
    public string[]? Usings { get; set; } --> any `usings` that need to be added to a C# file. 
    public string? FileName { get; set; } --> .cs file to edit
    public string[]? ClassProperties { get; set; } --> any properties that need to be added to the class' members.
    public string[]? ClassAttributes { get; set; } --> [Attribute] that need to added to the class.
}

class Method
{
    public string[]? Parameters { get; set; } --> parameter types for the method. Used to get the correct ParameterSyntax.
    public CodeChange[]? CodeChanges { get; set; } --> All the changes within a particular method.
}
class CodeChange
{
    public string? InsertAfter { get; set; } --> Insert new statement block after this statement syntax node.
    public string? Block { get; set; } --> C# statement that is parsed using SyntaxFactory.ParseStatement
    public string? Parent { get; set; } --> Add C# statement syntax node upon this parent statement syntax node based on Type 
    public string? Type { get; set; } --> CodeChangeType (below) string.
    public bool? Append { get; set; } = false; --> Insert Block at the top of the method.
}

class CodeChangeType
{
    public const string MemberAccess = nameof(MemberAccess); --> Add a SimpleMemberAccess expression to the parent statement syntax. 
    public const string InLambdaBlock = nameof(InBlock); --> Add in lambda block to the parent statement syntax. 
}
This info is also available in CodeModifierConfig folder as well.

The scenarios below need to be supported for all project types :
| Scenario | Status|
| --- | --- |
| ASP .NET Core Web App | Config w/out layout(.cshtml) files | 
| ASP .NET Core Web App (w/ B2C tenant) | Config w/out layout(.cshtml) files | 
| ASP .NET Core Web Api | Config w/out layout(.cshtml) files | 
| ASP .NET Core Web Api (w/ B2C tenant) | Config w/out layout(.cshtml) files | 
| Blazor Server App | Need config |
| Blazor Server App (w/ B2C tenant) | Need config |
| Blazor WebAssembly App | Need config |
| Blazor WebAssembly App (w/ B2C tenant) | Need config | 
| Blazor Hosted WebAssembly App | Need config |
| Blazor Hosted WebAssembly App (w/ B2C tenant) | Need config | 

Blazor server updates-
- App.razor changes
+ Shared/LoginDisplay.razor
- Shared/MainLayout.razor changes
- Startup.cs changes
- appsettings.json changes
