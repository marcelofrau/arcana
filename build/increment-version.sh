#!/usr/bin/env bash
# Increment the build counter in build-counter.txt (Linux/macOS)
set -euo pipefail
COUNTER_FILE="$(dirname "$0")/build-counter.txt"
COUNTER=$(cat "$COUNTER_FILE")
COUNTER=$((COUNTER + 1))
printf '%d' "$COUNTER" > "$COUNTER_FILE"
echo "Build counter incremented to $COUNTER"
