# Grim Dawn Item Assistant for Linux

This is a direct port of the [original IAGD](https://github.com/marius00/iagd) for Linux!  
It reuses ~95% of the original backend code, and changes to the original will be able to be synced to this project.  
Changes had to be made for the Linux specific paths and the wine prefix. The UI had to be rewritten using Avalonia.

For an AppImage download, see [the latest release](https://github.com/elmodor/iagd/releases)

Currently not yet ported:  
Character Backup when using steam cloud

## Requirements
For running this project you need `webkit2gtk-4.1` installed:  
Arch: `webkit2gtk-4.1`  
Debian/Ubuntu: `libwebkit2gtk-4.1-0`  
Fedora: `webkit2gtk4.1`  
openSUSE: `libwebkit2gtk-4_1-0`

Furtheremore you have to set `WINEDLLOVERRIDES="winmm=n,b" %command%` as your launch option in steam (or similar if you start it differently)

## Linux specifics
Your data and items are saved according to XDG specc in `.local/share/IAGrim`.  
The hook is injected via a dll loader `winmm`. It is automatically installed to your Grim Dawn install directories along with `ItemAssistantHook_x64.dll`. The loader will also load `DPYes` if the dll is present.  
> [!IMPORTANT]
> Item Assistant currently will automatically loot your items, even if you are not running the AppImage. The items will be stored in the wine prefix and will be stored into the database once you run the application.


### Uninstall
The best way is to remove the checkbox for `Use Dll Hooks` and click `Delete Dll Hooks` in the settings tab. You can also manually delete `winmm.dll` and `ItemAssistantHook_x64.dll` from your `x64` and `compat` directory. If you do not do this, Item Assistant will continue to loot your items!

# Links
[Original IAGD for Windows](https://github.com/marius00/iagd)
