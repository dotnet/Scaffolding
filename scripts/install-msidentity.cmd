set VERSION=1.0.0-dev
set SRC_DIR=%cd%
set NUPKG=artifacts/packages/Debug/Shipping/

call taskkill /f /im dotnet.exe
call rd /Q /S artifacts

call dotnet build MSIdentityScaffolding.slnf
call dotnet pack MSIdentityScaffolding.slnf
call dotnet tool uninstall -g Microsoft.dotnet-msidentity

call cd  %SRC_DIR%/%NUPKG% 
call dotnet tool install -g Microsoft.dotnet-msidentity --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call cd %SRC_DIR%