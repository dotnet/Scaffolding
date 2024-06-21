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
dotnet tool uninstall -g Microsoft.dotnet-scaffold
cd "$OLDPWD"/$NUPKG 
dotnet tool install -g Microsoft.dotnet-scaffold --add-source $SRC_DIR/$NUPKG  --version $VERSION
cd "$OLDPWD"