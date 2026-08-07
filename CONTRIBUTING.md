# Contributing to .NET Scaffolding

Thank you for your interest in contributing to .NET Scaffolding! This document provides guidelines and instructions for contributing to this repository.

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Contributor License Agreement](#contributor-license-agreement)
- [Ways to Contribute](#ways-to-contribute)
- [Getting Started](#getting-started)
- [Repository Structure](#repository-structure)
- [Development Workflow](#development-workflow)
- [Making Code Changes](#making-code-changes)
- [Coding Conventions](#coding-conventions)
- [Testing Your Changes](#testing-your-changes)
- [Debugging](#debugging)
- [Submitting Your Changes](#submitting-your-changes)
- [Reporting Issues](#reporting-issues)
- [Additional Resources](#additional-resources)
- [License](#license)

---

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](CODE-OF-CONDUCT.md). For more information see the Code of Conduct FAQ or contact opencode@microsoft.com with any additional questions or comments.

---

## Contributor License Agreement

Most contributions require you to agree to a **Contributor License Agreement (CLA)** declaring that you have the right to, and actually do, grant us the rights to use your contribution.

- When you open a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (for example, with a status check or comment).
- You only need to sign the CLA **once** across all Microsoft/.NET Foundation repositories.
- Simply follow the instructions provided by the bot and complete the signing process before your PR can be merged.

For details, see https://cla.opensource.microsoft.com/.

---

## Ways to Contribute

There are many ways to contribute beyond writing code:

- **Report bugs**: File clear, reproducible [issues](#reporting-issues).
- **Fix bugs**: Look for issues labeled `bug` or `help wanted`. Before you start, **reproduce the bug yourself first** to confirm it still occurs on the latest `main` — some issues are already fixed or environment-specific. Capture the exact reproduction steps so you can re-run them after your fix to verify it's resolved.
- **Add or update templates and scaffolders**: See [Making Code Changes](#making-code-changes).
- **Improve documentation**: Fix typos, clarify instructions, or add examples in the `docs/` folder and READMEs.
- **Write tests**: Increase coverage for existing functionality.
- **Answer questions**: Help others in GitHub Discussions and Issues.

If you're new, look for issues tagged **`good first issue`** or **`help wanted`** — these are curated to be approachable. For anything non-trivial, please open or comment on an issue first so we can align on the approach before you invest significant time.

---

## Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

1. **Preview .NET SDK**: 
   - Download a preview version that matches the version in `global.json` at the root of the repository
   - Reference: https://github.com/dotnet/sdk/blob/main/documentation/package-table.md
   
2. **Development Environment**:
   - Visual Studio 2022 (latest preview) **OR**
   - Visual Studio Code with C# Dev Kit extension
   
3. **Git**: For cloning the repository and version control

4. **Azure CLI** (if working on Azure-related features):
   ```bash
   az login
   ```

### Cloning the Repository

```bash
git clone https://github.com/dotnet/Scaffolding.git
cd Scaffolding
```

We recommend cloning under your user profile directory for easier access.

---

## Repository Structure

Understanding the repository structure is crucial for making contributions:

```
Scaffolding/
├── src/
│   ├── dotnet-scaffolding/
│   │   └── dotnet-scaffold/
│   │       ├── AspNet/              # ASP.NET scaffolders
│   │       │   ├── Templates/       # Templates organized by .NET version
│   │       │   │   ├── net8.0/
│   │       │   │   ├── net9.0/
│   │       │   │   ├── net10.0/     # .NET 10 templates
│   │       │   │   │   ├── BlazorCrud/
│   │       │   │   │   ├── BlazorEntraId/
│   │       │   │   │   ├── BlazorIdentity/
│   │       │   │   │   ├── Identity/
│   │       │   │   │   ├── MinimalApi/
│   │       │   │   │   ├── RazorPages/
│   │       │   │   │   ├── Views/
│   │       │   │   │   └── CodeModificationConfigs/  # JSON configs for code changes
│   │       │   │   └── net11.0/
│   │       │   ├── ScaffoldSteps/   # Step implementations
│   │       │   ├── Commands/        # Command definitions
│   │       │   └── Helpers/         # Helper utilities
│   │       ├── Aspire/              # Aspire scaffolders
│   │       │   └── CodeModificationConfigs/
│   │       │       └── net11.0/     # Aspire code modification configs
│   │       └── ...
│   ├── MSIdentityScaffolding/       # dotnet msidentity tool
│   └── Scaffolding/                 # Legacy scaffolding (maintenance mode)
├── test/
│   ├── dotnet-scaffolding/
│   │   └── dotnet-scaffold.Tests/
│   │       └── AspNet/              # Unit tests for ASP.NET scaffolders
│   └── MSIdentityScaffolding/
├── docs/                            # Documentation
│   ├── Getting-Started.md
│   └── ...
├── scripts/                         # Build and install scripts
│   ├── install-scaffold.cmd
│   └── install-scaffold.sh
└── All.sln                          # Main solution file
```

### Key Directories

- **Templates**: Located in `src/dotnet-scaffolding/dotnet-scaffold/AspNet/Templates/{version}/`
  - Contains T4 templates (`.tt` files) for generating code
  - Organized by .NET version (net8.0, net9.0, net10.0, net11.0)
  - Each scaffolder type has its own subfolder

- **CodeModificationConfigs**: Located in:
  - `src/dotnet-scaffolding/dotnet-scaffold/AspNet/Templates/{version}/CodeModificationConfigs/`
  - `src/dotnet-scaffolding/dotnet-scaffold/Aspire/CodeModificationConfigs/{version}/`
  - Contains JSON files that define code modifications
  - Examples: `blazorEntraChanges.json`, `identityChanges.json`

- **ScaffoldSteps**: Located in `src/dotnet-scaffolding/dotnet-scaffold/AspNet/ScaffoldSteps/`
  - Contains the logic for each scaffolding step
  - Implement the `ScaffoldStep` base class

- **Tests**: Located in `test/dotnet-scaffolding/dotnet-scaffold.Tests/AspNet/`
  - Unit tests for scaffolding functionality
  - Mirror the structure of the source code

---

## Development Workflow

### 1. Set Up Your Development Environment

1. Open the solution in your IDE:
   ```bash
   # Visual Studio
   start All.sln
   
   # Visual Studio Code
   code .
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

> **Tip:** The repository also ships convenience scripts at the root that restore and build with the pinned SDK from `global.json`:
> ```bash
> build.cmd    # Windows
> ./build.sh   # macOS/Linux
> ```
> To launch a fully configured IDE that uses the repo-local SDK, use `startvs.cmd` (Visual Studio) or `start-code.cmd` (VS Code) instead of opening the solution directly. This ensures the correct preview SDK is picked up.

### 2. Install Local Development Packages

After making changes, install your local build to test:

**Windows (cmd):**
```cmd
scripts\install-scaffold.cmd
```

**macOS/Linux or Windows (PowerShell):**
```bash
scripts/install-scaffold.sh
```

These scripts automate the full local install cycle. Under the hood they:

1. Kill any running `dotnet.exe` processes (so the tool isn't locked).
2. Clear the previous `artifacts` output and pack a fresh NuGet package via `dotnet pack` (Debug configuration).
3. Uninstall the existing global `Microsoft.dotnet-scaffold` tool.
4. Purge the cached scaffolding packages from your NuGet cache (`Microsoft.dotnet-scaffold`, `Microsoft.DotNet.Scaffolding.Internal`, `Microsoft.DotNet.Scaffolding.Core`) so the new build isn't shadowed by a cached copy.
5. Reinstall the tool globally from your freshly built local package, making `dotnet scaffold` point at your changes.

Because the tool is installed **globally**, you must re-run the install script after **every** change you want to test — a plain `dotnet build` updates the binaries in `artifacts/` but does **not** update the globally installed `dotnet scaffold` command.

### 3. Development Loop

This is the core inner loop you'll repeat while working:

1. **Make Changes**: Edit the code, templates, or configs.
2. **Reinstall**: Run the install script for your platform to rebuild, repack, and reinstall the global tool:
   ```cmd
   scripts\install-scaffold.cmd    :: Windows (cmd)
   ```
   ```bash
   scripts/install-scaffold.sh     # macOS/Linux
   ```
   (The script builds for you, so a separate `dotnet build` step isn't required before running it.)

   **Verify the install script completed successfully.** Watch the output and confirm it finishes without errors — the final `dotnet tool install -g Microsoft.dotnet-scaffold ...` step should report that the tool was installed. If the script fails partway (for example, because a `dotnet` process was still locking files, or the pack step errored), your changes won't be deployed. Re-run the script after resolving the error, and sanity-check the active version with:
   ```bash
   dotnet scaffold --version
   ```
3. **Test**: Run `dotnet scaffold ...` against a test project to exercise your change (see [Testing Your Changes](#testing-your-changes)).
4. **Repeat**: Iterate until the behavior is correct.

> **Important:** If you skip the install step, `dotnet scaffold` will keep running the **previously installed** version and you won't see your changes. When in doubt, re-run the install script. If results still look stale, delete `.config` folders and clear the NuGet cache (see [Common Issues](#common-issues)).

---

## Making Code Changes

### Working with Templates

Templates are the source files used to generate code in user projects.

#### Locating Templates

1. Navigate to `src/dotnet-scaffolding/dotnet-scaffold/AspNet/Templates/`
2. Choose the .NET version folder you're updating (e.g., `net10.0/`, `net11.0/`)
3. Find the scaffolder type subfolder (e.g., `BlazorEntraId/`, `Identity/`)
4. Edit the `.tt` (T4 template) files

**Example**: Updating Blazor Entra ID templates for .NET 10:
```
src/dotnet-scaffolding/dotnet-scaffold/AspNet/Templates/net10.0/BlazorEntraId/
├── AuthenticationStateProvider.tt
├── LoginDisplay.tt
└── ...
```

#### Template Guidelines

- **Test your templates**: Ensure generated code compiles and runs
- **Follow C# conventions**: Use proper naming, formatting, and patterns
- **Add comments**: Include XML documentation for public APIs
- **Parameterize properly**: Use template parameters for dynamic values
- **Keep it simple**: Templates should be readable and maintainable

### Working with Code Modification Configs

Code modification configs are JSON files that define how to modify existing code files.

#### Locating Config Files

**For ASP.NET scaffolders:**
```
src/dotnet-scaffolding/dotnet-scaffold/AspNet/Templates/{version}/CodeModificationConfigs/
```

**For Aspire scaffolders:**
```
src/dotnet-scaffolding/dotnet-scaffold/Aspire/CodeModificationConfigs/{version}/
```

**Example Config Files:**
- `blazorEntraChanges.json` - Blazor Entra ID code modifications
- `identityChanges.json` - ASP.NET Identity modifications
- `minimalApiChanges.json` - Minimal API modifications

#### Config File Structure

```json
{
  "Files": [
    {
      "FilePath": "Program.cs",
      "Usings": [
        "Microsoft.AspNetCore.Authentication",
        "Microsoft.Identity.Web"
      ],
      "CodeChanges": [
        {
          "Block": "GlobalStatements",
          "CodeSnippet": "builder.Services.AddAuthentication(...);"
        }
      ]
    }
  ]
}
```

#### Config Guidelines

- **Validate JSON**: Ensure your JSON is well-formed
- **Test modifications**: Verify code changes apply correctly
- **Be specific**: Use precise code blocks and insertion points
- **Handle edge cases**: Consider different project structures

### Adding New Scaffolders

1. Create a new subfolder in `AspNet/Templates/{version}/`
2. Add your T4 templates
3. Create a code modification config if needed
4. Implement scaffold steps in `AspNet/ScaffoldSteps/`
5. Register your scaffolder in `AspNetCommandService.cs`
6. Add unit tests in `test/.../AspNet/`

### Modifying Existing Scaffolders

1. Locate the scaffolder in `AspNet/Templates/{version}/`
2. Edit the appropriate template files or config files
3. Update corresponding scaffold steps if needed
4. Update existing tests or add new ones
5. Test thoroughly

---

## Coding Conventions

Consistent code makes review faster and the codebase easier to maintain.

### Style and Formatting

- **Follow `.editorconfig`**: The repository root contains an [`.editorconfig`](.editorconfig) that defines indentation, spacing, `using` ordering, and naming rules. Most IDEs apply it automatically.
- **Match the surrounding code**: Prefer the patterns already used in the file you're editing.
- **Format before committing**: Run `dotnet format` to apply style fixes and catch violations early:
  ```bash
  dotnet format All.sln
  ```
- **General C# guidance**: Follow the [.NET runtime coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) where this repo does not specify otherwise.

### Naming and Documentation

- Use `PascalCase` for types and public members, `camelCase` for locals and parameters, and `_camelCase` for private fields.
- Add XML documentation comments (`///`) to public APIs.
- Keep methods focused and avoid unrelated changes in the same PR.

### Commit Messages

- Write clear, imperative-mood subject lines (e.g., "Add Entra ID logout template" rather than "Added" or "Adds").
- Keep the subject under ~72 characters and add a body explaining *why* when the change isn't obvious.
- Reference related issues in the body (e.g., `Fixes #123`).

### Branch Naming

Use branch names in the format `dev/<alias>/<description>` — the literal `dev`, then your user alias, then a kebab-case description of the bug or feature after the second `/`:

```
dev/jdoe/blazor-identity-logout
dev/jdoe/minimal-api-null-ref
dev/jdoe/contributing-updates
```

---

## Testing Your Changes

Testing is **required** for all contributions. Contributors must add unit tests for new functionality.

### Manual Testing

#### Step 1: Create a Test Project

```bash
# Create a test Blazor project
dotnet new blazorserver -n TestApp -f net10.0
cd TestApp
```

#### Step 2: Run the Scaffolder

```bash
# Example: Add Entra ID authentication
dotnet scaffold aspnet entra-id --help

# Or run without options for interactive mode
dotnet scaffold aspnet entra-id
```

#### Step 3: Verify the Results

1. Check that files were created correctly
2. Ensure the project builds:
   ```bash
   dotnet build
   ```
3. Run the project and test functionality:
   ```bash
   dotnet run
   ```

#### Step 4: Clean Up

```bash
cd ..
rm -rf TestApp
```

### Unit Testing

**All code changes must include unit tests.**

#### Locating Tests

Tests are organized to mirror the source structure:
```
test/dotnet-scaffolding/dotnet-scaffold.Tests/AspNet/
├── Helpers/
├── ScaffoldSteps/
└── ...
```

#### Writing Tests

1. Create or locate the appropriate test file
2. Use xUnit framework (already configured)
3. Follow existing test patterns
4. Test both success and failure scenarios

**Example Test Structure:**
```csharp
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet
{
    public class MyScaffolderTests
    {
        [Fact]
        public void Should_GenerateCorrectCode_When_ValidInput()
        {
            // Arrange
            var input = "test";
            
            // Act
            var result = MyScaffolder.Generate(input);
            
            // Assert
            Assert.NotNull(result);
            Assert.Contains("expected", result);
        }
        
        [Fact]
        public void Should_ThrowException_When_InvalidInput()
        {
            // Arrange
            var input = "";
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                MyScaffolder.Generate(input));
        }
    }
}
```

#### Running Tests

**You must run the test suite and confirm it passes before opening a pull request.** A PR with failing or unrun tests will not be merged, and CI will run these same tests on your branch.

```bash
# Run the entire test suite (do this before every PR)
dotnet test All.sln

# Run a single test project (faster inner loop)
dotnet test test/dotnet-scaffolding/dotnet-scaffold.Tests/dotnet-scaffold.Tests.csproj

# Run only the tests related to your change using a filter
dotnet test test/dotnet-scaffolding/dotnet-scaffold.Tests/dotnet-scaffold.Tests.csproj --filter "FullyQualifiedName~BlazorEntra"

# Run with detailed output to diagnose a failure
dotnet test test/dotnet-scaffolding/dotnet-scaffold.Tests/dotnet-scaffold.Tests.csproj -v detailed
```

**Example — full pre-PR test run:**
```bash
# From the repository root
dotnet test All.sln -c Debug
```
Confirm the summary reports `Failed: 0` for every test project before you push and open your PR. If any test fails, fix it (or update the test if the behavior intentionally changed) before submitting.

> **Note:** Some end-to-end integration tests are known to be flaky or are intentionally skipped (for example, `net11`/preview-SDK scaffolding tests and tests needing real Azure AD credentials). Before assuming a failure is caused by your change, check [docs/KNOWN_FLAKY_TESTS.md](docs/KNOWN_FLAKY_TESTS.md) and re-run the test in isolation.

#### Test Guidelines

- **Write tests first** (TDD approach recommended)
- **Test edge cases**: Empty inputs, nulls, invalid data
- **Use descriptive names**: `Should_DoSomething_When_Condition`
- **Keep tests focused**: One assertion per test when possible
- **Mock dependencies**: Use mocking for external dependencies
- **Test both paths**: Success and failure scenarios

### Integration Testing

For larger changes, perform integration testing:

1. Create multiple test projects (Blazor Server, Blazor WASM, MVC, etc.)
2. Run scaffolders on each project type
3. Build and run each project
4. Verify functionality end-to-end

---

## Debugging

### Using Visual Studio

1. Open `All.sln` in Visual Studio
2. Set breakpoints in your code
3. Add the following line near the top of the entry point you want to debug:
   ```csharp
   System.Diagnostics.Debugger.Launch();
   ```
4. Run the install script to deploy your changes
5. Execute `dotnet scaffold` from a terminal
6. The debugger will launch automatically - attach to the process

### Using Visual Studio Code

1. Open the repository in VS Code
2. Install the C# Dev Kit extension
3. Create a launch configuration (`.vscode/launch.json`)
4. Set breakpoints
5. Use F5 to start debugging

### Common Issues

- **`.config` folders**: Delete `.config` folders in both the scaffolding repo and test projects
- **Overlapping SDK versions**: Ensure only one version of a preview SDK is installed
- **Package cache**: Clear NuGet cache if experiencing package issues:
  ```bash
  dotnet nuget locals all --clear
  ```

---

## Submitting Your Changes

### Before Submitting

1. ✅ **Build succeeds**: `dotnet build` completes without errors
2. ✅ **All tests pass**: `dotnet test` shows all green
3. ✅ **New tests added**: Unit tests cover your changes
4. ✅ **Manual testing done**: Verified with actual scaffolding scenarios
5. ✅ **Code formatted**: `dotnet format` run and [conventions](#coding-conventions) followed
6. ✅ **Documentation updated**: Update relevant docs if needed
7. ✅ **CLA signed**: [Contributor License Agreement](#contributor-license-agreement) completed

### Creating a Pull Request

1. **Fork the repository** (if you haven't already)
   
2. **Create a feature branch**:
   ```bash
   git checkout -b feature/my-awesome-feature
   ```

3. **Make your changes** and commit:
   ```bash
   git add .
   git commit -m "Add awesome feature for Blazor scaffolding"
   ```

4. **Push to your fork**:
   ```bash
   git push origin feature/my-awesome-feature
   ```

5. **Open a Pull Request** on GitHub:
   - Provide a clear title and description
   - Reference any related issues
   - Describe what you changed and why
   - Include testing steps

### Pull Request Guidelines

- **Clear description**: Explain what changes you made and why
- **Link issues**: Reference related GitHub issues
- **Small PRs**: Keep changes focused and manageable
- **Test coverage**: Include test results or screenshots
- **Documentation**: Update docs if you changed behavior
- **Follow feedback**: Be responsive to code review comments

### Code Review Process

1. A maintainer will review your PR
2. Address any feedback or requested changes
3. Once approved, your changes will be merged
4. Your contribution will be included in the next release!

---

## Reporting Issues

### Security Issues

**Do not report security issues publicly.**

Report security issues and bugs privately to:
- **Email**: secure@microsoft.com
- **Response time**: Within 24 hours
- **More info**: [Security TechCenter](https://technet.microsoft.com/en-us/security/ff852094.aspx)

### Bug Reports

When reporting bugs, please include:

1. **Description**: Clear description of the bug
2. **Steps to reproduce**:
   ```
   1. Run dotnet new blazorserver
   2. Run dotnet scaffold aspnet entra-id
   3. See error...
   ```
3. **Expected behavior**: What you expected to happen
4. **Actual behavior**: What actually happened
5. **Environment**:
   - OS: Windows 11, macOS 14, etc.
   - .NET SDK version: `dotnet --version`
   - Scaffolding version
6. **Logs/Error messages**: Full error output
7. **Project type**: Blazor Server, MVC, Web API, etc.

### Feature Requests

For feature requests:
1. Check if it already exists in issues
2. Provide a clear use case
3. Describe the expected behavior
4. Consider implementation approach
5. Be open to discussion and alternatives

---

## Additional Resources

### Documentation

- **Getting Started**: [docs/Getting-Started.md](docs/Getting-Started.md)
- **Known Flaky/Skipped Tests**: [docs/KNOWN_FLAKY_TESTS.md](docs/KNOWN_FLAKY_TESTS.md)
- **Entra ID Scaffolder**: [docs/ENTRA_ID_SCAFFOLDER_DOCUMENTATION.md](docs/ENTRA_ID_SCAFFOLDER_DOCUMENTATION.md)
- **Main README**: [README.md](README.md)

### Related Projects

- [Microsoft Identity Web](https://github.com/AzureAD/microsoft-identity-web)
- [Entity Framework Core](https://github.com/dotnet/efcore)
- [ASP.NET Core](https://github.com/dotnet/aspnetcore)

### Getting Help

- **GitHub Issues**: For bugs and feature requests
- **GitHub Discussions**: For questions and community support
- **Stack Overflow**: Tag your questions with `dotnet-scaffolding`

---

## Quick Reference

### Common Commands

```bash
# Build the solution
dotnet build

# Run tests (run before every PR)
dotnet test All.sln

# Install local changes
scripts/install-scaffold.cmd   # Windows
scripts/install-scaffold.sh    # macOS/Linux

# Test your changes
cd ~/TestProject
dotnet scaffold aspnet --help
```

### Directory Quick Reference

| Component | Location |
|-----------|----------|
| ASP.NET Templates | `src/dotnet-scaffolding/dotnet-scaffold/AspNet/Templates/{version}/` |
| Code Modification Configs (ASP.NET) | `src/dotnet-scaffolding/dotnet-scaffold/AspNet/Templates/{version}/CodeModificationConfigs/` |
| Code Modification Configs (Aspire) | `src/dotnet-scaffolding/dotnet-scaffold/Aspire/CodeModificationConfigs/{version}/` |
| Scaffold Steps | `src/dotnet-scaffolding/dotnet-scaffold/AspNet/ScaffoldSteps/` |
| Unit Tests | `test/dotnet-scaffolding/dotnet-scaffold.Tests/AspNet/` |
| Documentation | `docs/` |

---

## License

By contributing to this repository, you agree that your contributions will be licensed under the same license that covers the project. See the [LICENSE](LICENSE) file for the full terms. Do not contribute code, templates, or assets that you do not have the right to license under these terms.

---

## Thank You!

Thank you for contributing to .NET Scaffolding! Your contributions help make .NET development better for everyone.

If you have questions or need help, don't hesitate to:
- Open a GitHub issue
- Start a discussion
- Reach out to the maintainers

Happy coding! 🚀
