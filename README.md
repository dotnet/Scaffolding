# ASP.NET Scaffolding

ASP.NET Scaffolding enables generating boilerplate code for web applications to speed up development.

To learn more about ASP.NET Scaffolding see http://go.microsoft.com/fwlink/?LinkId=820629

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the Home repo.

### Build instructions: 
- "build.cmd" builds, restores, packs and runs the tests.

### src:

### test:
- for test projects to run with arcade, include ".Tests" at the end of the test project name.

### artifacts:
- contains all binaries, logs, obj and package files. 
- the generated nupkgs are in artifacts/packages/$Configuration$/Shipping

### eng:
- Build.props :
- Signing.props : adding third-party, excluding, and including anything for signing purposes.

### global.json: 
set dotnet(core) and dotnet arcade sdk version
- dotnet(core) = use latest release version
- dotnet arcade sdk = still in preview versions. Use 
    https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet-tools to find an appropriate version.

### Additional Notes:
- project builds in VS fine but packages are not created(kinda crucial)
- cmd does not good error messages for failing test files. Use text explorer on VS to debug problems with tests.