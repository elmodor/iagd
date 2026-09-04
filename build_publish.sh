#!/bin/bash
set -e

rm -rf publish

PUBLISH="publish/IAGrim.AppDir"

./build_webui_tests.sh
./build_iagrim_tests.sh

./build_webui.sh
./build_hook.sh
./build_iagrim.sh "Release" $PUBLISH

find publish/ -type f -name '*.pdb' -delete

mkdir -p $PUBLISH/usr/share/icons/hicolor/256x256/apps/
mkdir -p $PUBLISH/usr/bin

cp IAGrim/gd.png "$PUBLISH/usr/share/icons/hicolor/256x256/apps/iagrim.png"
cp IAGrim/gd.png "$PUBLISH/iagrim.png"

cat > "$PUBLISH/usr/bin/iagrim" << EOF
#!/bin/sh

# Disable DMA-BUF, does not work on some gpus/systems
export WEBKIT_DISABLE_DMABUF_RENDERER="\${WEBKIT_DISABLE_DMABUF_RENDERER:-1}"

exec "\$(dirname "\$0")/../../opt/iagrim/IAGrim" "\$@"
EOF
chmod +x "$PUBLISH/usr/bin/iagrim"

cat > "$PUBLISH/iagrim.desktop" << EOF
[Desktop Entry]
Type=Application
Name=Grim Dawn Item Assistant
Exec=iagrim
Icon=iagrim
Categories=Utility;
Terminal=false
EOF

cat > "$PUBLISH/AppRun" << EOF
#!/bin/sh
exec "\$(dirname "\$0")/usr/bin/iagrim" "\$@"
EOF
chmod +x "$PUBLISH/AppRun"

APPIMAGETOOL="tools/appimagetool"
mkdir -p "tools"
if [ ! -x "$APPIMAGETOOL" ]; then
    curl -L "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage" -o "$APPIMAGETOOL"
fi
chmod +x $APPIMAGETOOL

mkdir -p $PUBLISH/opt/iagrim/Hook
cp HookDll/Hook/build/Hook.dll $PUBLISH/opt/iagrim/Hook/ItemAssistantHook_x64.dll
# TODO
cp winmm.dll $PUBLISH/opt/iagrim/Hook/winmm.dll

$APPIMAGETOOL $PUBLISH IAGrim-x86_64.AppImage
