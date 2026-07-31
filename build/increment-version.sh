#!/usr/bin/env bash
# Increment the build counter in build-counter.txt (Linux/macOS).
# Format: <prefix>|<counter> (e.g. 0.1.0|2). Counter resets to 1 when the prefix changes.
set -euo pipefail
COUNTER_FILE="$(dirname "$0")/build-counter.txt"
PROPS_FILE="$(dirname "$0")/../src/Directory.Build.props"

RAW=$(cat "$COUNTER_FILE")
STORED_PREFIX="${RAW%%|*}"
COUNTER=$(( "${RAW##*|}" ))

MAJOR=$(sed -n 's/.*<VersionMajor>\([^<]*\)<\/VersionMajor>.*/\1/p' "$PROPS_FILE")
MINOR=$(sed -n 's/.*<VersionMinor>\([^<]*\)<\/VersionMinor>.*/\1/p' "$PROPS_FILE")
PATCH=$(sed -n 's/.*<VersionPatch>\([^<]*\)<\/VersionPatch>.*/\1/p' "$PROPS_FILE")
PREFIX="$MAJOR.$MINOR.$PATCH"
if [ -z "$PREFIX" ]; then
  echo "ERROR: could not parse version components from $PROPS_FILE" >&2
  exit 1
fi

if [ "$PREFIX" != "$STORED_PREFIX" ]; then
  COUNTER=0
  echo "Version prefix changed: $STORED_PREFIX -> $PREFIX (counter reset)"
fi
COUNTER=$((COUNTER + 1))

printf '%s|%d' "$PREFIX" "$COUNTER" > "$COUNTER_FILE"
echo "Build counter incremented to $COUNTER ($PREFIX-build.$COUNTER)"
