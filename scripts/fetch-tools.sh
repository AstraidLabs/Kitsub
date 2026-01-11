#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VENDOR_DIR="$ROOT_DIR/vendor"
MANIFEST_PATH="$ROOT_DIR/src/Kitsub.Cli/ToolsManifest.json"

FFMPEG_VERSION="7.1"
MKVTOOLNIX_VERSION="87.0"
TOOLSET_VERSION="ffmpeg-${FFMPEG_VERSION}-mkvtoolnix-${MKVTOOLNIX_VERSION}"

FFMPEG_WIN_URL="https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
FFMPEG_LINUX_X64_URL="https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz"
FFMPEG_LINUX_ARM64_URL="https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-arm64-static.tar.xz"

MKVTOOLNIX_WIN_URL="https://mkvtoolnix.download/windows/releases/${MKVTOOLNIX_VERSION}/mkvtoolnix-64-bit-${MKVTOOLNIX_VERSION}.zip"
MKVTOOLNIX_LINUX_X64_URL="https://mkvtoolnix.download/linux/releases/${MKVTOOLNIX_VERSION}/mkvtoolnix-${MKVTOOLNIX_VERSION}-x86_64.AppImage"
MKVTOOLNIX_LINUX_ARM64_URL="https://mkvtoolnix.download/linux/releases/${MKVTOOLNIX_VERSION}/mkvtoolnix-${MKVTOOLNIX_VERSION}-aarch64.AppImage"

mkdir -p "$VENDOR_DIR"

fetch() {
  local url="$1"
  local output="$2"
  echo "Downloading $url"
  curl -L --fail --retry 3 -o "$output" "$url"
}

extract_ffmpeg_windows() {
  local archive="$1"
  local dest="$2"
  local temp_dir
  temp_dir="$(mktemp -d)"
  unzip -q "$archive" -d "$temp_dir"
  local ffmpeg_path
  ffmpeg_path="$(find "$temp_dir" -type f -name ffmpeg.exe | head -n 1)"
  local ffprobe_path
  ffprobe_path="$(find "$temp_dir" -type f -name ffprobe.exe | head -n 1)"
  mkdir -p "$dest"
  cp "$ffmpeg_path" "$dest/ffmpeg.exe"
  cp "$ffprobe_path" "$dest/ffprobe.exe"
  rm -rf "$temp_dir"
}

extract_ffmpeg_linux() {
  local archive="$1"
  local dest="$2"
  local temp_dir
  temp_dir="$(mktemp -d)"
  tar -xf "$archive" -C "$temp_dir"
  local ffmpeg_path
  ffmpeg_path="$(find "$temp_dir" -type f -name ffmpeg | head -n 1)"
  local ffprobe_path
  ffprobe_path="$(find "$temp_dir" -type f -name ffprobe | head -n 1)"
  mkdir -p "$dest"
  cp "$ffmpeg_path" "$dest/ffmpeg"
  cp "$ffprobe_path" "$dest/ffprobe"
  rm -rf "$temp_dir"
}

extract_mkvtoolnix_windows() {
  local archive="$1"
  local dest="$2"
  local temp_dir
  temp_dir="$(mktemp -d)"
  unzip -q "$archive" -d "$temp_dir"
  local mkvmerge_path
  mkvmerge_path="$(find "$temp_dir" -type f -name mkvmerge.exe | head -n 1)"
  local mkvpropedit_path
  mkvpropedit_path="$(find "$temp_dir" -type f -name mkvpropedit.exe | head -n 1)"
  mkdir -p "$dest"
  cp "$mkvmerge_path" "$dest/mkvmerge.exe"
  cp "$mkvpropedit_path" "$dest/mkvpropedit.exe"
  rm -rf "$temp_dir"
}

extract_mkvtoolnix_appimage() {
  local archive="$1"
  local dest="$2"
  chmod +x "$archive"
  "$archive" --appimage-extract > /dev/null
  local squash_root="$PWD/squashfs-root"
  mkdir -p "$dest"
  cp "$squash_root/usr/bin/mkvmerge" "$dest/mkvmerge"
  cp "$squash_root/usr/bin/mkvpropedit" "$dest/mkvpropedit"
  rm -rf "$squash_root"
}

process_rid() {
  local rid="$1"
  local ffmpeg_url="$2"
  local mkvtoolnix_url="$3"

  local rid_dir="$VENDOR_DIR/$rid"
  mkdir -p "$rid_dir"

  local ffmpeg_archive="$rid_dir/ffmpeg.tar"
  fetch "$ffmpeg_url" "$ffmpeg_archive"
  if [[ "$rid" == "win-x64" ]]; then
    extract_ffmpeg_windows "$ffmpeg_archive" "$rid_dir/ffmpeg"
  else
    extract_ffmpeg_linux "$ffmpeg_archive" "$rid_dir/ffmpeg"
  fi
  rm -f "$ffmpeg_archive"

  local mkv_archive="$rid_dir/mkvtoolnix.bin"
  fetch "$mkvtoolnix_url" "$mkv_archive"
  if [[ "$rid" == "win-x64" ]]; then
    extract_mkvtoolnix_windows "$mkv_archive" "$rid_dir/mkvtoolnix"
  else
    extract_mkvtoolnix_appimage "$mkv_archive" "$rid_dir/mkvtoolnix"
  fi
  rm -f "$mkv_archive"
}

process_rid "win-x64" "$FFMPEG_WIN_URL" "$MKVTOOLNIX_WIN_URL"
process_rid "linux-x64" "$FFMPEG_LINUX_X64_URL" "$MKVTOOLNIX_LINUX_X64_URL"
process_rid "linux-arm64" "$FFMPEG_LINUX_ARM64_URL" "$MKVTOOLNIX_LINUX_ARM64_URL"

python3 - <<PY
import hashlib
import json
from pathlib import Path

root = Path("$ROOT_DIR")
manifest_path = Path("$MANIFEST_PATH")
vendor = Path("$VENDOR_DIR")

def sha256(path: Path) -> str:
    hasher = hashlib.sha256()
    with path.open('rb') as fh:
        for chunk in iter(lambda: fh.read(1024 * 1024), b''):
            hasher.update(chunk)
    return hasher.hexdigest()

manifest = {
    "toolsetVersion": "$TOOLSET_VERSION",
    "rids": {}
}

rids = ["win-x64", "linux-x64", "linux-arm64"]
for rid in rids:
    ffmpeg = vendor / rid / "ffmpeg"
    mkvtoolnix = vendor / rid / "mkvtoolnix"
    if rid.startswith("win"):
        ffmpeg_name = "ffmpeg.exe"
        ffprobe_name = "ffprobe.exe"
        mkvmerge_name = "mkvmerge.exe"
        mkvpropedit_name = "mkvpropedit.exe"
    else:
        ffmpeg_name = "ffmpeg"
        ffprobe_name = "ffprobe"
        mkvmerge_name = "mkvmerge"
        mkvpropedit_name = "mkvpropedit"

    manifest["rids"][rid] = {
        "ffmpeg": {
            "path": f"ffmpeg/{ffmpeg_name}",
            "sha256": sha256(ffmpeg / ffmpeg_name)
        },
        "ffprobe": {
            "path": f"ffmpeg/{ffprobe_name}",
            "sha256": sha256(ffmpeg / ffprobe_name)
        },
        "mkvmerge": {
            "path": f"mkvtoolnix/{mkvmerge_name}",
            "sha256": sha256(mkvtoolnix / mkvmerge_name)
        },
        "mkvpropedit": {
            "path": f"mkvtoolnix/{mkvpropedit_name}",
            "sha256": sha256(mkvtoolnix / mkvpropedit_name)
        }
    }

manifest_path.write_text(json.dumps(manifest, indent=2))
print(f"Wrote manifest to {manifest_path}")
PY

echo "Tools staged under $VENDOR_DIR"
