#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="${1:?Usage: $0 <version> [arch]}"
ARCH="${2:-x64}"

# Strip leading v prefix if present
VERSION="${VERSION#v}"

# Detect OS for RID
case "$(uname -s)" in
  Darwin) OS="osx" ;;
  Linux)  OS="linux" ;;
  *)      echo "Unsupported OS: $(uname -s)" >&2; exit 1 ;;
esac
RID="$OS-$ARCH"

DIST_DIR="$ROOT/build/dist"
ZIP_NAME="Arcana-v$VERSION-$RID.zip"
ZIP_PATH="$DIST_DIR/$ZIP_NAME"

# Skip PublishReadyToRun for arm64 cross-compile
R2R="true"
[ "$ARCH" = "arm64" ] && R2R="false"

echo "Building Arcana v$VERSION for $RID..."
mkdir -p "$DIST_DIR"

for proj in "$ROOT/src/Arcana.App/Arcana.App.csproj" "$ROOT/src/Arcana.Cli/Arcana.Cli.csproj"; do
    name="$(basename "${proj%.csproj}")"
    dotnet publish "$proj" \
        -c Release \
        -r "$RID" \
        --self-contained true \
        -p:PublishReadyToRun="$R2R" \
        -p:Version="$VERSION" \
        -o "$DIST_DIR/$name"
done

echo "Packaging $ZIP_NAME..."
cd "$DIST_DIR" && zip -r "$ZIP_PATH" Arcana.App Arcana.Cli && cd "$ROOT"

rm -rf "$DIST_DIR/Arcana.App" "$DIST_DIR/Arcana.Cli"

echo "Release created: $ZIP_PATH"
