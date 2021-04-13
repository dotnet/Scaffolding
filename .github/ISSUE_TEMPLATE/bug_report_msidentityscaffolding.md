---
name: Bug in Microsoft.DotNet.Identity/dotnet-msidentity
about: Create a report about something that isn't working
labels: area-msidentity, customer-reported
---

## File a bug

Remember:

* Please check that the [documentation](https://aka.ms/dotnet-msidentity) does not explain the behavior you are seeing.
* Please search in both [open](https://github.com/dotnet/scaffolding/issues) and [closed](https://github.com/dotnet/Scaffolding/issues?q=is%3Aissue+is%3Aclosed) issues to check that your bug has not already been filed.

### Include your code

To fix any bug we must first reproduce it. To make this possible, please attach a small, runnable project or post a small, runnable code listing that reproduces what you are seeing.

It is often impossible for us to reproduce a bug when working with only code snippets since we have to guess at the missing code.

Use triple-tick code fences for any posted code. For example:

```C#
Console.WriteLine("Hello, World!");
```

### Include stack traces

Include the full exception message and stack trace for any exception you encounter.

Use triple-tick fences for stack traces. For example:

```
Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
   at Bane.NoSurvivors() in C:\Batman\Foes\Bane.cs:line 420
   at Program.Main() in C:\Batman\Program.cs:line 69
```

### Include version information

Microsoft.VisualStudio.Web.CodeGeneration.Design/Microsoft.DotNet.MSIdentity version:
Target framework: (e.g. .NET 5.0)
Operating system:
IDE: (e.g. Visual Studio 2019 16.3)
