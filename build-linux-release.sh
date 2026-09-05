#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

VERSION="$(tr -d '\r\n ' < VERSION)"
CHANNEL="$(tr '[:upper:]' '[:lower:]' < RELEASE_CHANNEL | tr -d '\r\n ')"

if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "VERSION must use MAJOR.MINOR.PATCH. Got: $VERSION" >&2
  exit 1
fi
if [[ "$CHANNEL" != "beta" && "$CHANNEL" != "stable" ]]; then
  echo "RELEASE_CHANNEL must be beta or stable. Got: $CHANNEL" >&2
  exit 1
fi
if [[ "${VERSION%%.*}" == "0" && "$CHANNEL" != "beta" ]]; then
  echo "Every Ghost FTP 0.x build must remain Beta." >&2
  exit 1
fi

OUTPUT="$ROOT/release-linux"
WORK="$ROOT/artifacts/linux-release"
rm -rf "$OUTPUT" "$WORK"
mkdir -p "$OUTPUT" "$WORK"

write_installer() {
  local path="$1"
  cat > "$path" <<'INSTALLER'
#!/usr/bin/env bash
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
BIN_HOME="${XDG_BIN_HOME:-$HOME/.local/bin}"
APP_HOME="$DATA_HOME/ghostftp"
APPLICATIONS_HOME="$DATA_HOME/applications"

mkdir -p "$APP_HOME" "$BIN_HOME" "$APPLICATIONS_HOME"
install -m 0755 "$HERE/GhostFTP" "$APP_HOME/GhostFTP"
ln -sfn "$APP_HOME/GhostFTP" "$BIN_HOME/ghostftp"

cat > "$APPLICATIONS_HOME/ghostftp.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=Ghost FTP
Comment=Private FTP and FTPS file transfer client
Exec=$APP_HOME/GhostFTP
Terminal=false
Categories=Network;FileTransfer;
StartupNotify=true
DESKTOP
chmod 0644 "$APPLICATIONS_HOME/ghostftp.desktop"

printf '%s\n' "Ghost FTP installed for the current user." \
  "Application: $APP_HOME/GhostFTP" \
  "Command: $BIN_HOME/ghostftp" \
  "Desktop entry: $APPLICATIONS_HOME/ghostftp.desktop" \
  "" \
  "No telemetry or tracking service is installed. Saved profiles/settings remain in the current user's local application-data directory."
INSTALLER
  chmod 0755 "$path"
}

write_uninstaller() {
  local path="$1"
  cat > "$path" <<'UNINSTALLER'
#!/usr/bin/env bash
set -euo pipefail

DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
BIN_HOME="${XDG_BIN_HOME:-$HOME/.local/bin}"
APP_HOME="$DATA_HOME/ghostftp"
APPLICATIONS_HOME="$DATA_HOME/applications"

rm -f "$BIN_HOME/ghostftp" "$APPLICATIONS_HOME/ghostftp.desktop"
rm -rf "$APP_HOME"

if [[ "${1:-}" == "--purge" ]]; then
  rm -rf "$DATA_HOME/GhostFTP" "$DATA_HOME/ghostftp-data"
  echo "Ghost FTP removed together with local application data requested by --purge."
else
  echo "Ghost FTP application files removed. Local profiles/settings were preserved."
fi
UNINSTALLER
  chmod 0755 "$path"
}

publish_rid() {
  local rid="$1"
  local publish_dir="$WORK/publish-$rid"
  local package_name="GhostFTP-$VERSION-$CHANNEL-$rid"
  local stage="$WORK/$package_name"

  echo "Publishing Ghost FTP $VERSION $CHANNEL for $rid..."
  dotnet publish src/GhostFTP.Linux/GhostFTP.Linux.csproj \
    -c Release \
    -r "$rid" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$publish_dir"

  if [[ ! -f "$publish_dir/GhostFTP" ]]; then
    echo "Linux publish did not produce GhostFTP for $rid." >&2
    exit 1
  fi

  mkdir -p "$stage"
  install -m 0755 "$publish_dir/GhostFTP" "$stage/GhostFTP"
  install -m 0644 LICENSE "$stage/LICENSE"
  install -m 0644 docs/PLATFORM-SUPPORT.md "$stage/PLATFORM-SUPPORT.md"
  install -m 0644 docs/CODE-SIGNING.md "$stage/CODE-SIGNING.md"
  write_installer "$stage/install.sh"
  write_uninstaller "$stage/uninstall.sh"

  cat > "$stage/README-LINUX.txt" <<EOF_README
Ghost FTP $VERSION $CHANNEL — Linux ($rid)

BRENDIGO LTD
https://ghostftp.com

Run directly:
  ./GhostFTP

Install for the current Linux user:
  ./install.sh

Uninstall application files while preserving local profiles/settings:
  ./uninstall.sh

Purge application files and Ghost FTP local data:
  ./uninstall.sh --purge

Runtime desktop requirement:
  Standard X11 client library (libX11.so.6). Wayland desktops can run Ghost FTP through XWayland.

Privacy:
  Ghost FTP contains no analytics, telemetry or tracking SDK. FTP/FTPS credentials and saved profiles are stored locally on the user's device. Linux saved-password protection uses AES-256-GCM with a random user-private local key file (0600 where the filesystem supports Unix permissions).
EOF_README

  tar -C "$WORK" -czf "$OUTPUT/$package_name.tar.gz" "$package_name"
  cp "$stage/GhostFTP" "$OUTPUT/GhostFTP-$rid"
  chmod 0755 "$OUTPUT/GhostFTP-$rid"

  echo "Created $OUTPUT/$package_name.tar.gz"
  echo "Created $OUTPUT/GhostFTP-$rid"
}

publish_rid linux-x64
publish_rid linux-arm64

cp "$OUTPUT/GhostFTP-$VERSION-$CHANNEL-linux-x64.tar.gz" "$OUTPUT/GhostFTP-linux-x64.tar.gz"
cp "$OUTPUT/GhostFTP-$VERSION-$CHANNEL-linux-arm64.tar.gz" "$OUTPUT/GhostFTP-linux-arm64.tar.gz"

(
  cd "$OUTPUT"
  sha256sum \
    GhostFTP-linux-x64 \
    GhostFTP-linux-arm64 \
    GhostFTP-linux-x64.tar.gz \
    GhostFTP-linux-arm64.tar.gz \
    GhostFTP-$VERSION-$CHANNEL-linux-x64.tar.gz \
    GhostFTP-$VERSION-$CHANNEL-linux-arm64.tar.gz \
    > SHA256SUMS-linux.txt
)

cat > "$OUTPUT/BUILD-INFO.txt" <<EOF_INFO
Ghost FTP $VERSION $CHANNEL Linux release
Publisher: BRENDIGO LTD
Targets: linux-x64, linux-arm64
Framework: .NET 10 self-contained single-file publish
UI: direct X11/XWayland renderer using the platform libX11 client library
Third-party NuGet packages: none
Telemetry/tracking SDKs: none
EOF_INFO

echo "Linux release packaging completed: $OUTPUT"
