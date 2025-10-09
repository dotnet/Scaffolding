# How to Build `dotnet scaffold`

This guide is based on my experience installing and running the `dotnet scaffold` repository locally.

## 📦 Repository Location

Clone the repository from GitHub:

```
https://github.com/dotnet/Scaffolding
```

I recommend cloning it under your user profile directory.

## 🚀 Getting Started: Dev loop

1. **Navigate** to the cloned `Scaffolding` directory. Edit as needed.

2. **Install dev package**

   from cmd: run `scripts\install-scaffold.cmd`
   ***OR***
   from powershell run: `scripts\install-scaffold.sh`

   These scripts will install the Nuget Packages associated with your local changes on your machine.

3. **Test changes**

   From a cmd (or powershell), test your changes locally. Run `dotnet scaffold` or the command you are trying to test. For debugging testing, attach the debugger as outlined below and repeat these steps. 


## 🧪 Debugging Setup

1. Open the `all.sln` solution in a separate instance of Visual Studio.
2. Set any breakpoints you want to hit.
3. Before running the install script, add the following line near the top of the project you want to debug (e.g., `dotnet-scaffold-aspnet`, etc.):

   ```csharp
   System.Diagnostics.Debugger.Launch();
   ```

   This will automatically launch a debugger. Otherwise, you’ll need to attach one manually.

4. In a terminal, navigate to a project that is ready for scaffolding (a relatively blank project with a basic model class).
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
