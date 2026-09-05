using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace IAGrim.Overwrites {
    public static class HookFiles {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(HookFiles));
        private static readonly string IagdDll = Path.Combine(AppContext.BaseDirectory, "Hook", "ItemAssistantHook_x64.dll");
        private static readonly string WinmmDll = Path.Combine(AppContext.BaseDirectory, "Hook", "winmm.dll");

        public static void UpdateHookFiles(string grimDawnLocation)
        {
            if (string.IsNullOrWhiteSpace(grimDawnLocation))
                return;
            var x64Dir = Path.Combine(grimDawnLocation, "x64");
            var grimDawnExe = Path.Combine(x64Dir, "Grim Dawn.exe");
            if (File.Exists(grimDawnExe)) {
                BackupExisting(x64Dir);
                CopyIfNewer(IagdDll, Path.Combine(x64Dir, "ItemAssistantHook_x64.dll"));
                CopyIfNewer(WinmmDll, Path.Combine(x64Dir, "winmm.dll"));
            }
            var compatDir = Path.Combine(grimDawnLocation, "compat");
            grimDawnExe = Path.Combine(compatDir, "Grim Dawn.exe");
            if (File.Exists(grimDawnExe)) {
                BackupExisting(compatDir);
                CopyIfNewer(IagdDll, Path.Combine(compatDir, "ItemAssistantHook_x64.dll"));
                CopyIfNewer(WinmmDll, Path.Combine(compatDir, "winmm.dll"));
            }
        }

        public static void UpdateHookFiles(IEnumerable<string> grimDawnLocations)
        {
            foreach (var location in grimDawnLocations.Distinct())
                UpdateHookFiles(location);
        }

        private static void CopyIfNewer(string source, string destination) {
            if (!File.Exists(source)) {
                Logger.Error($"Hook file does not exist: {source}");
                return;
            }
            if (File.Exists(destination) && File.GetLastWriteTimeUtc(destination) >= File.GetLastWriteTimeUtc(source)) {
                return;
            }
            try {
                File.Copy(source, destination, overwrite: true);
                Logger.Info($"Updated hook file {destination}");
            }
            catch (Exception ex) {
                Logger.Error($"Failed to copy {source} to {destination}", ex);
            }
        }

        private static void BackupExisting(string location) {
            if (!Path.Exists(location))
                return;
            var iagdDll = Path.Combine(location, "ItemAssistantHook_x64.dll");
            var winmmDll = Path.Combine(location, "winmm.dll");
            var winmmDllOrig = Path.Combine(location, "winmm.dll.orig");
            if (!File.Exists(iagdDll) && File.Exists(winmmDll) && !File.Exists(winmmDllOrig)) {
                File.Move(winmmDll, winmmDllOrig);
                Logger.Info($"Backed up {winmmDll}");
            }
        }

        private static void RestoreExisting(string location) {
            if (!Path.Exists(location))
                return;
            var winmmDll = Path.Combine(location, "winmm.dll");
            var winmmDllOrig = Path.Combine(location, "winmm.dll.orig");
            if (File.Exists(winmmDllOrig) && !File.Exists(winmmDll)) {
                File.Move(winmmDllOrig, winmmDll);
                Logger.Info($"Restored {winmmDll}");
            }
        }

        public static void DeleteHookFiles(string grimDawnLocation)
        {
            if (string.IsNullOrWhiteSpace(grimDawnLocation))
                return;
            var x64Dir = Path.Combine(grimDawnLocation, "x64");
            var grimDawnExe = Path.Combine(x64Dir, "Grim Dawn.exe");
            if (File.Exists(grimDawnExe)) {
                File.Delete(Path.Combine(x64Dir, "ItemAssistantHook_x64.dll"));
                File.Delete(Path.Combine(x64Dir, "winmm.dll"));
                RestoreExisting(x64Dir);
            }
            var compatDir = Path.Combine(grimDawnLocation, "compat");
            grimDawnExe = Path.Combine(compatDir, "Grim Dawn.exe");
            if (File.Exists(grimDawnExe)) {
                File.Delete(Path.Combine(compatDir, "ItemAssistantHook_x64.dll"));
                File.Delete(Path.Combine(compatDir, "winmm.dll"));
                RestoreExisting(compatDir);
            }
        }

        public static void DeleteHookFiles(IEnumerable<string> grimDawnLocations)
        {
            foreach (var location in grimDawnLocations.Distinct())
                DeleteHookFiles(location);
        }
    }
}
