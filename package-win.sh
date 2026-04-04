#!/bin/bash
set -e

APP_NAME="QuillStone"
APP_VERSION="1.0.0"
MANUFACTURER="bopera"
# Stable upgrade GUID — never change this or Windows will treat updates as new installs
UPGRADE_CODE="A1B2C3D4-E5F6-7890-ABCD-EF1234567890"

ARCH="${1:-x64}"  # pass "arm64" as first arg for ARM, default x64
RID="win-$ARCH"
PUBLISH_DIR="QuillStone/bin/Release/net10.0/$RID/publish"
OUT_DIR="$APP_NAME-win-$ARCH"

echo "Publishing for $RID..."
dotnet publish QuillStone/QuillStone.csproj -c Release -r "$RID" --self-contained true

echo "Creating package folder..."
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"
cp -r "$PUBLISH_DIR/"* "$OUT_DIR/"

echo "Zipping..."
zip -r "$ZIP" "$OUT_DIR"
rm -rf "$OUT_DIR"

echo "Done! Transfer $ZIP to Windows and extract — run $APP_NAME.exe inside."
echo "Tip: on Windows run package-win.ps1 to build an MSI installer."
