set VERSION=6.0.0-dev
set DEFAULT_NUPKG_PATH=%userprofile%/.nuget/packages
set SRC_DIR=%cd%
set NUPKG=artifacts/packages/Debug/Shipping/
call taskkill /f /im dotnet.exe
call rd /Q /S artifacts
call dotnet build Scaffolding.slnf
call dotnet pack Scaffolding.slnf 
call dotnet tool uninstall -g dotnet-aspnet-codegenerator 

call cd %DEFAULT_NUPKG_PATH%
call rd /Q /S microsoft.visualstudio.web.codegeneration
call rd /Q /S microsoft.visualstudio.web.codegeneration.contracts
call rd /Q /S microsoft.visualstudio.web.codegeneration.core
call rd /Q /S microsoft.visualstudio.web.codegeneration.design
call rd /Q /S microsoft.visualstudio.web.codegeneration.entityframeworkcore
call rd /Q /S microsoft.visualstudio.web.codegeneration.templating
call rd /Q /S microsoft.visualstudio.web.codegeneration.utils
call rd /Q /S microsoft.visualstudio.web.codegenerators.mvc

call cd  %SRC_DIR%/%NUPKG% 
call dotnet tool install -g dotnet-aspnet-codegenerator --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call cd %SRC_DIR%