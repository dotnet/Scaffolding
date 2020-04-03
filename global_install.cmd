set VERSION=
set SRC_DIR=%cd%
set NUPKG=artifacts/packages/Debug/Shipping/
call taskkill /f /im dotnet.exe
call git clean -xdf
call build.cmd  
call dotnet tool uninstall -g dotnet-aspnet-codegenerator 
call cd  %NUPKG% 
call dotnet tool install -g dotnet-aspnet-codegenerator --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
call cd %SRC_DIR%