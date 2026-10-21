#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
source "$project_dir/scripts/game-paths.sh"
game_app="$SOR_GAME_APP"
loader_dir="$project_dir/.tools/loader"
if [[ "$game_app" == *.app ]]; then
    executable_name="$(plutil -extract CFBundleExecutable raw -o - "$game_app/Contents/Info.plist")"
    game_executable="$game_app/Contents/MacOS/$executable_name"
else
    game_executable="$game_app"
fi
if [ ! -x "$game_executable" ]; then
    echo "Game executable not found: $game_executable"
    exit 1
fi
if pgrep -f '/StreetsOfRogueMac.app/Contents/MacOS/Streets of Rogue' >/dev/null; then
    echo 'Close the running Streets of Rogue first, then launch this again.'
    exit 1
fi
if [ ! -f "$loader_dir/BepInEx/plugins/BurritoRun.dll" ]; then
    echo 'Prototype is not built yet. Run: bash scripts/build.sh'
    exit 1
fi
export BURRITO_PROFILE_PATH="${BURRITO_PROFILE_PATH:-$project_dir/.runtime/profile}"
log_dir="$(dirname "$BURRITO_PROFILE_PATH")/logs"
mkdir -p "$BURRITO_PROFILE_PATH/CloudData" "$BURRITO_PROFILE_PATH/BackupData" "$log_dir"
if [ ! -f "$BURRITO_PROFILE_PATH/.initialized" ]; then
    original_profile="$SOR_ORIGINAL_PROFILE"
    for name in GameUnlocks.dat GameSettings.dat; do
        if [ -f "$original_profile/$name" ]; then
            cp -p "$original_profile/$name" "$BURRITO_PROFILE_PATH/CloudData/$name"
        fi
    done
    touch "$BURRITO_PROFILE_PATH/.initialized"
fi
export SteamAppId=512900
export SteamGameId=512900
stamp="$(date '+%Y%m%d-%H%M%S')-$$"
log="$log_dir/game-$stamp.log"
echo 'Burrito Run: launching with a separate prototype save profile.'
echo "Session log: $log"
if [[ " $* " == *' --world-harness '* ]]; then
    echo 'Native harness will start the run and collect evidence automatically.'
elif [[ " $* " == *' --banana-favors '* ]]; then
    echo 'Start a single-player run. Meet the marked banana neighbors and choose one reward.'
else
    echo 'Quick Start a single-player run. Order at any computer.'
fi
cd "$loader_dir"
# Doorstop's relative dylib resolution requires this working directory on macOS.
set +e
bash run_bepinex.sh "$game_executable" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "$log" "$@" >"$log_dir/console-$stamp.log" 2>&1
result=$?
set -e
cp "$loader_dir/BepInEx/LogOutput.log" "$log_dir/plugin-$stamp.log" 2>/dev/null || true
if [ "$result" -ne 0 ]; then
    echo "Game exited with code $result. Logs are saved in .runtime/logs."
fi
exit "$result"
