#!/bin/bash

VERSION=5.0.0
DEFAULT_NUPKG_PATH=~/.nuget/packages
SRC_DIR=$(pwd)
echo $SRC_DIR
NUPKG=artifacts/packages/Debug/Shipping/

#kill all dotnet procs
pkill -f dotnet
rm -rf artifacts
./build.sh 
dotnet tool uninstall -g Microsoft.dotnet-scaffold
cd $DEFAULT_NUPKG_PATH
rm -rf microsoft.visualstudio.web.codegeneration
rm -rf Microsoft.DotNet.Scaffolding.Shared
rm -rf microsoft.visualstudio.web.codegeneration.core
rm -rf microsoft.visualstudio.web.codegeneration.design
rm -rf microsoft.visualstudio.web.codegeneration.entityframeworkcore
rm -rf microsoft.visualstudio.web.codegeneration.templating
rm -rf microsoft.visualstudio.web.codegeneration.utils
rm -rf microsoft.visualstudio.web.codegenerators.mvc
cd "$OLDPWD"/$NUPKG 
dotnet tool install -g Microsoft.dotnet-scaffold --add-source $SRC_DIR/$NUPKG  --version $VERSION
cd "$OLDPWD"