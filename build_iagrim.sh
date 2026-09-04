#!/bin/bash
set -e

export MSBUILDDISABLENODEREUSE=1

CONFIGURATION="${1:-Debug}"
PUBLISH="${2:-}"

if [ "$CONFIGURATION" = "Release" ]; then
    dotnet publish IAGrim/IAGrim.csproj \
        -c Release \
        --self-contained true \
        -o "$PUBLISH/opt/iagrim/"
else
    dotnet build IAGrim/IAGrim.csproj \
        -c "$CONFIGURATION"
fi
