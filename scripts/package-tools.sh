#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VENDOR_DIR="$ROOT_DIR/vendor"
TOOLS_DIR="$ROOT_DIR/src/Kitsub.Cli/tools"
ARCHIVES_DIR="$ROOT_DIR/src/Kitsub.Cli/tools-archives"

mkdir -p "$TOOLS_DIR" "$ARCHIVES_DIR"

rids=("win-x64" "linux-x64" "linux-arm64")

for rid in "${rids[@]}"; do
  echo "Packaging $rid"
  rm -rf "$TOOLS_DIR/$rid"
  mkdir -p "$TOOLS_DIR/$rid"
  cp -R "$VENDOR_DIR/$rid/ffmpeg" "$TOOLS_DIR/$rid/ffmpeg"
  cp -R "$VENDOR_DIR/$rid/mkvtoolnix" "$TOOLS_DIR/$rid/mkvtoolnix"

  mkdir -p "$ARCHIVES_DIR"
  (cd "$TOOLS_DIR/$rid" && zip -r -q "$ARCHIVES_DIR/$rid.zip" .)
  echo "Created archive $ARCHIVES_DIR/$rid.zip"
done

echo "Tools staged under $TOOLS_DIR"
