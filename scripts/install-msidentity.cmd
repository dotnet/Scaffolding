set VERSION=1.0.0-dev
set NUPKG=artifacts\packages\Debug\Shipping\

pushd %~dp0
call cd C:\Scaffolding\
set SRC_DIR=C:\Scaffolding\artifacts\packages\Debug\Shipping\

call taskkill /f /im dotnet.exe
call rd /Q /S artifacts

call build
call dotnet tool uninstall -g Microsoft.dotnet-msidentity
call cd ~\.nuget\packages
call rd /Q /S microsoft.dotnet.scaffolding.shared
call rd /Q /S microsoft.dotnet-msidentity
call cd C:\Scaffolding\
call dotnet tool install -g Microsoft.dotnet-msidentity --add-source %SRC_DIR% --version %VERSION%
popd