set VERSION=9.0.1-dev
set DEFAULT_NUPKG_PATH=%userprofile%/.nuget/packages
set SRC_DIR=%cd%
set NUPKG=artifacts/packages/Debug/Shipping/
call taskkill /f /im dotnet.exe
call rd /Q /S artifacts
call build.cmd
call dotnet tool uninstall -g Microsoft.dotnet-scaffold
call dotnet tool uninstall -g Microsoft.dotnet-scaffold-aspire
call dotnet tool uninstall -g Microsoft.dotnet-scaffold-aspnet

call cd %DEFAULT_NUPKG_PATH%
call rd /Q /S Microsoft.dotnet-scaffold
call rd /Q /S Microsoft.dotnet-scaffold-aspire
call rd /Q /S Microsoft.dotnet-scaffold-aspnet
call rd /Q /S Microsoft.DotNet.Scaffolding.Internal
call rd /Q /S Microsoft.DotNet.Scaffolding.Core

call cd  %SRC_DIR%/%NUPKG%
call dotnet tool install -g Microsoft.dotnet-scaffold --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call dotnet tool install -g Microsoft.dotnet-scaffold-aspire --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call dotnet tool install -g Microsoft.dotnet-scaffold-aspnet --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call cd %SRC_DIR%
