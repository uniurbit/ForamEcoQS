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
    linux-x64|linux-arm64) echo "$ROOT/src/ForamEcoQS.Gtk/ForamEcoQS.Gtk.csproj" ;;
    osx-x64|osx-arm64)     echo "$ROOT/src/ForamEcoQS.Mac/ForamEcoQS.Mac.csproj" ;;
    win-x64|win-arm64)     echo "$ROOT/src/ForamEcoQS.Wpf/ForamEcoQS.Wpf.csproj" ;;
    *)       echo "" ;;
  esac
}

make_app_bundle() {
  local rid="$1"
  local bundle="$ARTIFACTS/$rid/ForamEcoQS.app"

  mkdir -p "$bundle/Contents/Resources"
  cp "$ROOT/src/ForamEcoQS.App/Resources/Icon.icns" "$bundle/Contents/Resources/"

  local version
  version="$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$ROOT/Directory.Build.props" | head -1)"

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
    <key>CFBundleIconFile</key>        <string>Icon.icns</string>
    <key>NSHighResolutionCapable</key> <true/>
    <key>NSHumanReadableCopyright</key><string>MIT License - ForamEcoQS authors</string>
</dict>
</plist>
PLIST

  chmod +x "$bundle/Contents/MacOS/ForamEcoQS"
  echo "    bundle: $bundle"

  # Archive the bundle before uploading it: raw CI artifacts lose executable permissions.
  if [[ "$(uname -s)" == Darwin ]]; then
    ditto -c -k --sequesterRsrc --keepParent "$bundle" "$ARTIFACTS/$rid/ForamEcoQS-$rid.zip"
  else
    # Windows filesystems may not retain chmod on the extensionless macOS launcher.
    # Set its archive mode explicitly with GNU tar (Linux / Git Bash).
    local archive="$ARTIFACTS/$rid/ForamEcoQS-$rid.tar"
    local launcher="ForamEcoQS.app/Contents/MacOS/ForamEcoQS"
    tar -cf "$archive" --mode=755 -C "$ARTIFACTS/$rid" "$launcher"
    tar -rf "$archive" --exclude="$launcher" -C "$ARTIFACTS/$rid" ForamEcoQS.app
    gzip -f "$archive"
  fi
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

  publish_dir="$out"
  publish_options=(--self-contained false)
  if [[ "$rid" == osx-* ]]; then
    # Publish directly into the final bundle, including the runtime and reference data.
    # Disable Eto's second bundler so it cannot create nested or duplicate .app folders.
    publish_dir="$out/ForamEcoQS.app/Contents/MacOS"
    publish_options=(--self-contained true -p:MacBuildBundle=false -p:MacAutoPublishBundle=false)
  fi

  dotnet publish "$project" \
    -c "$CONFIG" \
    -r "$rid" \
    "${publish_options[@]}" \
    -o "$publish_dir" \
    -v quiet --nologo

  if [[ "$rid" == osx-* ]]; then
    make_app_bundle "$rid"
  fi

  echo "    output: $out"
done

echo
echo "All done. Artifacts are in $ARTIFACTS/"
