#!/bin/bash
set -e

APP_NAME="QuillStone"
ARCH="${1:-x64}"  # pass "arm64" as first arg for Apple Silicon, default x64
RID="osx-$ARCH"
BUNDLE="$APP_NAME.app"
PUBLISH_DIR="QuillStone/bin/Release/net10.0/$RID/publish"

echo "Publishing for $RID..."
dotnet publish QuillStone/QuillStone.csproj -c Release -r "$RID" --self-contained true

echo "Creating .app bundle..."
rm -rf "$BUNDLE"
mkdir -p "$BUNDLE/Contents/MacOS"
mkdir -p "$BUNDLE/Contents/Resources"

cp -r "$PUBLISH_DIR/"* "$BUNDLE/Contents/MacOS/"
cp QuillStone/Assets/Icons/icon.icns "$BUNDLE/Contents/Resources/$APP_NAME.icns"

cat > "$BUNDLE/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>com.bopera.quillstone</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

chmod +x "$BUNDLE/Contents/MacOS/$APP_NAME"

echo "Done! Open $BUNDLE or move it to /Applications."
