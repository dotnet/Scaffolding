set VERSION=9.0.0-dev
set DEFAULT_NUPKG_PATH=%userprofile%/.nuget/packages
set SRC_DIR=%cd%
set NUPKG=artifacts/packages/Debug/Shipping/
call taskkill /f /im dotnet.exe
call rd /Q /S artifacts
call dotnet pack src\dotnet-scaffolding\dotnet-scaffold\dotnet-scaffold.csproj -c Debug
call dotnet tool uninstall -g Microsoft.dotnet-scaffold

call cd %DEFAULT_NUPKG_PATH%
call rd /Q /S Microsoft.dotnet-scaffold
call rd /Q /S Microsoft.DotNet.Scaffolding.Internal
call rd /Q /S Microsoft.DotNet.Scaffolding.Core

call cd  %SRC_DIR%/%NUPKG% 
call dotnet tool install -g Microsoft.dotnet-scaffold --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call cd %SRC_DIR%