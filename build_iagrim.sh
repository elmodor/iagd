#!/bin/bash
set -e

export MSBUILDDISABLENODEREUSE=1

CONFIGURATION="${1:-Debug}"
PUBLISH="${2:-}"

if [ -n "${VERSION}" ]; then
    TAG="$VERSION"
else
    TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "1.1.2.2-linux.1")
fi
TAG="${TAG#v}"
if [[ "$TAG" =~ ^([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)-linux\.([0-9]+)$ ]]; then
    ORIGVERSION="${BASH_REMATCH[1]}"
    FORKREVISION="${BASH_REMATCH[2]}"
else
    exit 1
fi

if [ "$CONFIGURATION" = "Release" ]; then
    dotnet publish IAGrim/IAGrim.csproj \
        -p:Version=$ORIGVERSION -p:ForkRevision=$FORKREVISION \
        -c Release \
        --self-contained true \
        -o "$PUBLISH/opt/iagrim/"
else
    dotnet build IAGrim/IAGrim.csproj \
        -p:Version=$ORIGVERSION -p:ForkRevision="${FORKREVISION}-dev" \
        -c "$CONFIGURATION"
fi
