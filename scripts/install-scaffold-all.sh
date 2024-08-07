#!/bin/bash

VERSION=9.0.0-dev
DEFAULT_NUPKG_PATH=~/.nuget/packages
SRC_DIR=$(pwd)
echo $SRC_DIR
NUPKG=artifacts/packages/Debug/Shipping/

#kill all dotnet procs
pkill -f dotnet
rm -rf artifacts
dotnet pack src/dotnet-scaffolding/dotnet-scaffold/dotnet-scaffold.csproj -c Debug
dotnet pack src/dotnet-scaffolding/dotnet-scaffold-aspire/dotnet-scaffold-aspire.csproj -c Debug
dotnet pack src/dotnet-scaffolding/dotnet-scaffold-aspnet/dotnet-scaffold-aspnet.csproj -c Debug
dotnet tool uninstall -g Microsoft.dotnet-scaffold
dotnet tool uninstall -g Microsoft.dotnet-scaffold-aspire
dotnet tool uninstall -g Microsoft.dotnet-scaffold-aspnet
cd "$OLDPWD"/$NUPKG
dotnet tool install -g Microsoft.dotnet-scaffold --add-source $SRC_DIR/$NUPKG  --version $VERSION
dotnet tool install -g Microsoft.dotnet-scaffold-aspire --add-source $SRC_DIR/$NUPKG  --version $VERSION
dotnet tool install -g Microsoft.dotnet-scaffold-aspnet --add-source $SRC_DIR/$NUPKG  --version $VERSION
cd "$OLDPWD"
