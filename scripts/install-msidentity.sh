VERSION=1.0.0-dev
DEFAULT_NUPKG_PATH=~/.nuget/packages
SRC_DIR=$(pwd)
NUPKG=artifacts/packages/Debug/Shipping/
#kill all dotnet procs
pkill -f dotnet
rm -rf artifacts
./build.sh 
dotnet tool uninstall -g Microsoft.dotnet-msidentity
cd $SRC_DIR/$NUPKG 
dotnet tool install -g Microsoft.dotnet-msidentity --add-source $SRC_DIR/$NUPKG  --version $VERSION
cd "$OLDPWD"