#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
archive="$project_dir/.tools/downloads/BepInEx_macos_universal_5.4.23.5.zip"
loader_dir="$project_dir/.tools/loader"
mkdir -p "$project_dir/.tools/downloads" "$loader_dir"
if [ ! -f "$archive" ]; then
    curl -fL --retry 3 -o "$archive" 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_macos_universal_5.4.23.5.zip'
fi
expected='01c2ae782eb016dfd6c345a18dbd2dcafffb3d9d318449d6486689f426b4a323'
actual="$(shasum -a 256 "$archive" | cut -d ' ' -f 1)"
if [ "$actual" != "$expected" ]; then
    echo 'BepInEx archive checksum mismatch. Setup stopped.' >&2
    exit 1
fi
unzip -oq "$archive" -d "$loader_dir"
bash "$project_dir/scripts/build.sh" /p:UseSharedCompilation=false
echo 'Ready: double-click quick-test.command or play-burrito-run.command.'
