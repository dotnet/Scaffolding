set VERSION=1.0.0-dev
set NUPKG=artifacts\packages\Debug\Shipping\

pushd %~dp0
call cd ..
set SRC_DIR=%cd%

call taskkill /f /im dotnet.exe
call rd /Q /S artifacts

call dotnet build MSIdentityScaffolding.slnf
call dotnet pack MSIdentityScaffolding.slnf
call dotnet tool uninstall -g Microsoft.dotnet-msidentity
call dotnet tool install -g Microsoft.dotnet-msidentity --add-source %SRC_DIR%\%NUPKG% --version %VERSION%
popd