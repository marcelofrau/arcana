#!/usr/bin/env bash
# Build Arcana in Debug mode (Linux/macOS)
set -euo pipefail
dotnet build "$(dirname "$0")/../src/Arcana.slnx" -c Debug
