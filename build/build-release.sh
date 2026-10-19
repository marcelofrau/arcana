#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="${1:?Usage: build-release.sh version [x64|arm64]}"
VERSION="${VERSION#v}"
ARCH="${2:-x64}"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?$ ]] || { echo "Invalid version" >&2; exit 1; }
[[ "$ARCH" == x64 || "$ARCH" == arm64 ]] || { echo "Invalid architecture" >&2; exit 1; }
case "$(uname -s)" in
  Darwin) OS=osx ;;
  Linux) OS=linux ;;
  *) echo "Unsupported OS" >&2; exit 1 ;;
esac
RID="$OS-$ARCH"
DIST="$ROOT/build/dist"
mkdir -p "$DIST"
STAGE="$(mktemp -d "$DIST/stage-$RID-XXXXXX")"
trap 'rm -rf "$STAGE"' EXIT
for NAME in Arcana.App Arcana.Cli; do
  echo "Publishing $NAME $VERSION for $RID"
  dotnet publish "$ROOT/src/$NAME/$NAME.csproj" -c Release -r "$RID" --self-contained true \
    -p:PublishReadyToRun=false -p:Version="$VERSION" -p:VersionPrefix="$VERSION" \
    -p:InformationalVersion="$VERSION" -o "$STAGE/$NAME"
done
cp "$ROOT/LICENSE" "$STAGE/LICENSE"
cp "$ROOT/docs/ATTRIBUTIONS.md" "$STAGE/THIRD_PARTY_NOTICES.md"
cp "$ROOT/docs/PRODUCTION_READINESS.md" "$STAGE/RELEASE_NOTES.md"

HOST_ARCH="$(uname -m)"
if [[ "$ARCH:$HOST_ARCH" == x64:x86_64 || "$ARCH:$HOST_ARCH" == arm64:arm64 || "$ARCH:$HOST_ARCH" == arm64:aarch64 ]]; then
  pwsh -NoProfile -File "$ROOT/build/smoke-test.ps1" -CliPath "$STAGE/Arcana.Cli/Arcana.Cli"
fi
ZIP="$DIST/Arcana-v$VERSION-$RID.zip"
rm -f "$ZIP"
(cd "$STAGE" && zip -qr "$ZIP" Arcana.App Arcana.Cli LICENSE THIRD_PARTY_NOTICES.md RELEASE_NOTES.md)

if [[ "$OS" == linux ]]; then
  DEB="$STAGE/deb"
  mkdir -p "$DEB/DEBIAN" "$DEB/opt/arcana" "$DEB/usr/bin" "$DEB/usr/share/applications" "$DEB/usr/share/pixmaps"
  cp -a "$STAGE/Arcana.App" "$STAGE/Arcana.Cli" "$STAGE/LICENSE" "$STAGE/THIRD_PARTY_NOTICES.md" "$DEB/opt/arcana/"
  printf '#!/bin/sh\nexec /opt/arcana/Arcana.App/Arcana.App "$@"\n' > "$DEB/usr/bin/arcana-gui"
  printf '#!/bin/sh\nexec /opt/arcana/Arcana.Cli/Arcana.Cli "$@"\n' > "$DEB/usr/bin/arcana"
  chmod 755 "$DEB/usr/bin/arcana" "$DEB/usr/bin/arcana-gui"
  cp "$ROOT/build/linux/arcana.desktop" "$DEB/usr/share/applications/arcana.desktop"
  cp "$ROOT/docs/logo-icon.png" "$DEB/usr/share/pixmaps/arcana.png"
  DEB_ARCH=amd64
  [[ "$ARCH" == arm64 ]] && DEB_ARCH=arm64
  cat > "$DEB/DEBIAN/control" <<EOF
Package: arcana
Version: $VERSION
Architecture: $DEB_ARCH
Maintainer: Marcelo Frau <marcelofrau@gmail.com>
Section: utils
Priority: optional
Depends: libc6, libfontconfig1, libice6, libsm6, libx11-6
Description: Managed archive manager with an Avalonia desktop interface and CLI
EOF
  dpkg-deb --build --root-owner-group "$DEB" "$DIST/Arcana-v$VERSION-$RID.deb"
else
  APP="$STAGE/Arcana.app"
  mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
  cp -a "$STAGE/Arcana.App/." "$APP/Contents/MacOS/"
  cp -a "$STAGE/Arcana.Cli" "$APP/Contents/Resources/cli"
  cp "$STAGE/LICENSE" "$STAGE/THIRD_PARTY_NOTICES.md" "$APP/Contents/Resources/"
  sed "s/@VERSION@/${VERSION%%[-+]*}/g" "$ROOT/build/macos/Info.plist" > "$APP/Contents/Info.plist"
  ICONSET="$STAGE/Arcana.iconset"
  mkdir -p "$ICONSET"
  for SIZE in 16 32 128 256 512; do
    sips -z "$SIZE" "$SIZE" "$ROOT/docs/logo-icon.png" --out "$ICONSET/icon_${SIZE}x${SIZE}.png" >/dev/null
    DOUBLE=$((SIZE * 2))
    sips -z "$DOUBLE" "$DOUBLE" "$ROOT/docs/logo-icon.png" --out "$ICONSET/icon_${SIZE}x${SIZE}@2x.png" >/dev/null
  done
  iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/Arcana.icns"
  plutil -lint "$APP/Contents/Info.plist"
  # shortcut: ad-hoc signing supports local testing; public releases need Developer ID and notarization.
  codesign --force --deep --sign - "$APP"
  hdiutil create -ov -format UDZO -volname Arcana -srcfolder "$APP" "$DIST/Arcana-v$VERSION-$RID.dmg"
fi
echo "Packages created for $RID"
