#!/usr/bin/env bash
# One-time setup of a self-hosted GitHub Actions runner for AKeepersNeed2 on Debian/Ubuntu.
# Installs PowerShell + .NET SDK, creates a dedicated 'gha' user, registers the runner with
# labels self-hosted,linux,gk2, installs it as a systemd service and (optionally) installs
# the game's managed DLLs the build references.
#
# Usage (on the server):
#   sudo ./setup-runner.sh <registration-token> [dll-dir]
# Get the token from GitHub: repo Settings -> Actions -> Runners -> New self-hosted runner
# (it's the value after --token; valid for 1 hour). dll-dir holds the game DLLs to install.
set -euo pipefail

REPO_URL="https://github.com/Blo3m/AKeepersNeed2"
RUNNER_USER="gha"
RUNNER_DIR="/opt/actions-runner"
MANAGED_DIR="/opt/gk2/Managed"
DOTNET_DIR="/usr/share/dotnet"
PWSH_DIR="/opt/microsoft/powershell/7"

if [[ $EUID -ne 0 ]]; then
    echo "Run with sudo." >&2
    exit 1
fi
if [[ $# -lt 1 || $# -gt 2 ]]; then
    echo "Usage: sudo $0 <registration-token> [dll-dir]" >&2
    exit 1
fi
TOKEN="$1"
DLL_DIR="${2:-}"

case "$(uname -m)" in
    x86_64) ARCH="x64" ;;
    aarch64) ARCH="arm64" ;;
    *) echo "Unsupported arch $(uname -m)" >&2; exit 1 ;;
esac

latest_tag() {
    curl -fsSL "https://api.github.com/repos/$1/releases/latest" | grep -oP '"tag_name":\s*"v\K[^"]+'
}

apt-get update
# libicuNN is versioned per distro release, so pick whatever this one ships.
ICU_PKG="$(apt-cache search --names-only '^libicu[0-9]+$' | awk '{print $1}' | sort -V | tail -1)"
apt-get install -y curl tar ca-certificates libssl3t64 zlib1g libkrb5-3 "$ICU_PKG" \
    || apt-get install -y curl tar ca-certificates libssl3 zlib1g libkrb5-3 "$ICU_PKG"

# Microsoft's apt feed lags new Debian releases, so use the distro-independent installers.
if [[ ! -x "$DOTNET_DIR/dotnet" ]]; then
    curl -fsSLo /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$DOTNET_DIR"
    ln -sf "$DOTNET_DIR/dotnet" /usr/local/bin/dotnet
fi

if [[ ! -x "$PWSH_DIR/pwsh" ]]; then
    PWSH_VERSION="$(latest_tag PowerShell/PowerShell)"
    mkdir -p "$PWSH_DIR"
    curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${PWSH_VERSION}/powershell-${PWSH_VERSION}-linux-${ARCH}.tar.gz" \
        | tar xz -C "$PWSH_DIR"
    chmod +x "$PWSH_DIR/pwsh"
    ln -sf "$PWSH_DIR/pwsh" /usr/local/bin/pwsh
fi

if ! id "$RUNNER_USER" &>/dev/null; then
    useradd --create-home --shell /bin/bash "$RUNNER_USER"
fi

mkdir -p "$MANAGED_DIR"
if [[ -n "$DLL_DIR" ]]; then
    install -m 644 "$DLL_DIR"/*.dll "$MANAGED_DIR/"
fi
chown -R "$RUNNER_USER:$RUNNER_USER" "$(dirname "$MANAGED_DIR")"

mkdir -p "$RUNNER_DIR"
chown "$RUNNER_USER:$RUNNER_USER" "$RUNNER_DIR"
cd "$RUNNER_DIR"
if [[ ! -f ./config.sh ]]; then
    RUNNER_VERSION="$(latest_tag actions/runner)"
    curl -fsSL "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-${ARCH}-${RUNNER_VERSION}.tar.gz" \
        | sudo -u "$RUNNER_USER" tar xz
    # Its distro detection can trail new releases; the deps it wants are installed above.
    ./bin/installdependencies.sh || echo "installdependencies.sh failed; continuing" >&2
fi

if [[ ! -f ./.runner ]]; then
    sudo -u "$RUNNER_USER" ./config.sh \
        --unattended \
        --url "$REPO_URL" \
        --token "$TOKEN" \
        --name "$(hostname)-gk2" \
        --labels gk2
    ./svc.sh install "$RUNNER_USER"
fi
./svc.sh start

echo
echo "dotnet: $(dotnet --version)  pwsh: $(pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()')"
echo "Game DLLs in $MANAGED_DIR:"
ls -1 "$MANAGED_DIR"
