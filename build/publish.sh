#!/usr/bin/env bash
# Publishes ForamEcoQS for every supported desktop target.
#
#   ./build/publish.sh                 # every target this machine can build
#   ./build/publish.sh linux-x64       # one or more explicit runtime identifiers
#
# Output goes to artifacts/<rid>/. macOS builds also get a ForamEcoQS.app bundle.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS="$ROOT/artifacts"
CONFIG="${CONFIGURATION:-Release}"

ALL_RIDS=(linux-x64 linux-arm64 osx-x64 osx-arm64 win-x64 win-arm64)
RIDS=("$@")
if [ ${#RIDS[@]} -eq 0 ]; then
  RIDS=("${ALL_RIDS[@]}")
fi

project_for_rid() {
  case "$1" in
    linux-*) echo "$ROOT/src/ForamEcoQS.Gtk/ForamEcoQS.Gtk.csproj" ;;
    osx-*)   echo "$ROOT/src/ForamEcoQS.Mac/ForamEcoQS.Mac.csproj" ;;
    win-*)   echo "$ROOT/src/ForamEcoQS.Wpf/ForamEcoQS.Wpf.csproj" ;;
    *)       echo "" ;;
  esac
}

make_app_bundle() {
  local rid="$1" publish_dir="$2"
  local bundle="$ARTIFACTS/$rid/ForamEcoQS.app"

  rm -rf "$bundle"
  mkdir -p "$bundle/Contents/MacOS" "$bundle/Contents/Resources"

  cp -R "$publish_dir/." "$bundle/Contents/MacOS/"
  if [ -f "$ROOT/src/ForamEcoQS.App/Resources/favicon.ico" ]; then
    cp "$ROOT/src/ForamEcoQS.App/Resources/favicon.ico" "$bundle/Contents/Resources/"
  fi

  local version
  version="$(grep -oP '(?<=<Version>)[^<]+' "$ROOT/Directory.Build.props" | head -1)"

  cat > "$bundle/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>            <string>ForamEcoQS</string>
    <key>CFBundleDisplayName</key>     <string>ForamEcoQS</string>
    <key>CFBundleIdentifier</key>      <string>it.uniurb.foramecoqs</string>
    <key>CFBundleVersion</key>         <string>${version}</string>
    <key>CFBundleShortVersionString</key><string>${version}</string>
    <key>CFBundlePackageType</key>     <string>APPL</string>
    <key>CFBundleExecutable</key>      <string>ForamEcoQS</string>
    <key>NSHighResolutionCapable</key> <true/>
    <key>LSMinimumSystemVersion</key>  <string>11.0</string>
    <key>NSHumanReadableCopyright</key><string>MIT License - ForamEcoQS authors</string>
</dict>
</plist>
PLIST

  chmod +x "$bundle/Contents/MacOS/ForamEcoQS" 2>/dev/null || true
  echo "    bundle: $bundle"
}

for rid in "${RIDS[@]}"; do
  project="$(project_for_rid "$rid")"
  if [ -z "$project" ]; then
    echo "!! unknown runtime identifier: $rid" >&2
    exit 1
  fi

  out="$ARTIFACTS/$rid"
  echo "==> publishing $rid"
  rm -rf "$out"

  dotnet publish "$project" \
    -c "$CONFIG" \
    -r "$rid" \
    --self-contained false \
    -o "$out" \
    -v quiet --nologo

  if [[ "$rid" == osx-* ]]; then
    make_app_bundle "$rid" "$out"
  fi

  echo "    output: $out"
done

echo
echo "All done. Artifacts are in $ARTIFACTS/"
