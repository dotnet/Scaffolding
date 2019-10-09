ASP.NET Scaffolding enables generating boilerplate code for web applications to speed up development.

To learn more about ASP.NET Scaffolding see http://go.microsoft.com/fwlink/?LinkId=820629

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the Home repo.

Build instructions: "build.cmd" builds, restores, packs and runs the testss

src:

test:
- for test projects to run with arcade, include ".Tests" at the end of the test project name.

artifacts:
- contains all binaries, logs, obj and package files. 
- the generated nupkgs are in artifacts/packages/$Configuration$/Shipping
    - $Configuration = Debug or Release
- 
Notes:
- project builds in VS fine but packages are not created(kinda crucial)
- cmd does not good error messages for test. Use text explorer on VS to debug problems with tests.
