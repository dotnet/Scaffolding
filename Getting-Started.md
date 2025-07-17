# How to Build `dotnet scaffold`

This guide is based on my experience installing and running the `dotnet scaffold` repository locally.

## 📦 Repository Location

Clone the repository from GitHub:

```
https://github.com/dotnet/Scaffolding
```

I recommend cloning it under your user profile directory.

## 🚀 Getting Started

1. **Navigate** to the cloned `Scaffolding` directory.
2. **Run** `start-code.cmd`. This should launch your installed version of Visual Studio Code.
3. **From the terminal inside VS Code**, run:

   ```bash
   scripts\install-scaffold-all
   ```

   This command will:
   - Set the tool version to the dev version (distinguishing it from the published version).
   - Clear artifacts.
   - Build the project.
   - Run associated tests.
   - Install the NuGet package produced in the `artifacts` folder.

## 🧪 Debugging Setup

1. Open the `all.sln` solution in a separate instance of Visual Studio.
2. Set any breakpoints you want to hit.
3. Before running the install script, add the following line near the top of the project you want to debug (e.g., `dotnet-scaffold-aspnet`, `dotnet-scaffold-aspire`, etc.):

   ```csharp
   System.Diagnostics.Debugger.Launch();
   ```

   This will automatically launch a debugger. Otherwise, you’ll need to attach one manually.

4. In a **separate terminal**, navigate to a project that is ready for scaffolding (a relatively blank project with a basic model class).
5. Run `dotnet scaffold` from there.

## ✅ Requirements

- A **preview version** of the .NET SDK:
  - It should closely match the version specified in the `global.json` at the root of the scaffolding repo.
  - Reference: https://github.com/dotnet/sdk/blob/main/documentation/package-table.md
- **C# Dev Kit** (if using Visual Studio Code)

## ⚠️ Things to Watch Out For

- `.config` folders in the root of either the scaffolding project or the project you're scaffolding.
- **Missing or overlapping SDK versions**:
  - I encountered issues with two .NET 10.0 preview versions and had to delete the newer one.
