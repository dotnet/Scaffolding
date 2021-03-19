set VERSION=0.0.1-dev
set DEFAULT_NUPKG_PATH=%userprofile%/.nuget/packages
set SRC_DIR=%cd%
set NUPKG=artifacts/packages/Debug/Shipping/

call taskkill /f /im dotnet.exe
call rd /Q /S artifacts

call dotnet build MsIdentityScaffolding.slnf
call dotnet pack MsIdentityScaffolding.slnf
call dotnet tool uninstall -g dotnet-msidentity

call cd  %SRC_DIR%/%NUPKG% 
call dotnet tool install -g dotnet-msidentity --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call cd %SRC_DIR%