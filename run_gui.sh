#!/usr/bin/env sh
# run_gui.sh — Build and launch the Capstone Avalonia GUI
# Usage: ./run_gui.sh [--no-build]
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/src/Capstone.UI/Capstone.UI.csproj"
DOTNET="${DOTNET_ROOT:+$DOTNET_ROOT/dotnet}"
DOTNET="${DOTNET:-$(command -v dotnet 2>/dev/null || echo "$HOME/.dotnet/dotnet")}"

# Copy .env to the build output directory so the app can read IGDB credentials.
copy_env() {
    local bin_dir
    bin_dir="$SCRIPT_DIR/src/Capstone.UI/bin/Debug/net9.0"
    if [ -f "$SCRIPT_DIR/.env" ] && [ -d "$bin_dir" ]; then
        cp "$SCRIPT_DIR/.env" "$bin_dir/.env"
    fi
}

if [ "$1" != "--no-build" ]; then
    echo "==> Building Capstone.UI..."
    "$DOTNET" build "$PROJECT" --configuration Debug --nologo -v q
    copy_env
fi

echo "==> Starting Capstone GUI..."
exec "$DOTNET" run --project "$PROJECT" --configuration Debug --no-build
# run_gui.sh — Build and launch the Capstone Avalonia GUI
# Usage: ./run_gui.sh [--no-build]
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/src/Capstone.UI/Capstone.UI.csproj"
DOTNET="${DOTNET_ROOT:+$DOTNET_ROOT/dotnet}"
DOTNET="${DOTNET:-$(command -v dotnet 2>/dev/null || echo "$HOME/.dotnet/dotnet")}"

# Copy .env to the build output directory so the app can read IGDB credentials.
copy_env() {
    local bin_dir
    bin_dir="$SCRIPT_DIR/src/Capstone.UI/bin/Debug/net9.0"
    if [ -f "$SCRIPT_DIR/.env" ] && [ -d "$bin_dir" ]; then
        cp "$SCRIPT_DIR/.env" "$bin_dir/.env"
    fi
}

if [ "$1" != "--no-build" ]; then
    echo "==> Building Capstone.UI..."
    "$DOTNET" build "$PROJECT" --configuration Debug --nologo -v q
    copy_env
fi

echo "==> Starting Capstone GUI..."
exec "$DOTNET" run --project "$PROJECT" --configuration Debug --no-build
