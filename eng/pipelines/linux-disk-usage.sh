#!/usr/bin/env bash
set -euo pipefail

echo "Disk usage: $1"
df -h / "$BUILD_SOURCESDIRECTORY" "$AGENT_TEMPDIRECTORY"
for path in \
    "$BUILD_SOURCESDIRECTORY/.dotnet" \
    "$BUILD_SOURCESDIRECTORY/artifacts" \
    "${NUGET_PACKAGES:-$HOME/.nuget/packages}" \
    "$AGENT_TEMPDIRECTORY" \
    "$AGENT_TEMPDIRECTORY/scaffolding-sdk-staging"; do
    if [[ -e "$path" ]]; then
        du -sh "$path"
    fi
done
