using log4net;
using System.Linq;

namespace IAGrim.Utilities.Detection {
    class SteamDetection {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SteamDetection));

        public const string GrimDawnAppId = "219990";

        public required string SteamRoot { get; init; }
        public required IReadOnlyList<string> Libraries { get; init; }
        public string? GameDir { get; init; }
        public string? PrefixDir { get; init; }
        public string? SavePath { get; init; }
        public string? SaveSource { get; init; }

        public string? CompatDataDir => PrefixDir is null ? null : Path.GetDirectoryName(PrefixDir);
        public string? BridgeDir => PrefixDir is null ? null : BridgeDirIn(PrefixDir);
        public static string BridgeDirIn(string prefixDir) => Path.Combine(prefixDir, "drive_c", "users", "steamuser", "AppData", "Local", "EvilSoft", "IAGD");

        private static readonly string[] SteamRootCandidates = [
            "~/.local/share/Steam",
            "~/.steam/steam",
            "~/.steam/debian-installation",
            "~/.var/app/com.valvesoftware.Steam/data/Steam",
        ];

        // TODO return multiple / (game + remote)
        public static string? GetGrimSaveFolders() {
            var steamRoot = FindSteamRoot();
            Logger.Info($"Steam installation found: {steamRoot}");
            var libraries = ReadLibraryFolders(steamRoot);
            Logger.Info($"Found {libraries.Count} Steam library location(s)");
            var prefixDir = FindPrefix(libraries);
            if (!string.IsNullOrEmpty(prefixDir) && Directory.Exists(prefixDir)) {
                Logger.Info($"Grim Dawn Proton prefix found: {prefixDir}");
            }
            else {
                Logger.Warn("Could not find Grim Dawn Proton prefix");
            }
            var (savePath, saveSource) = FindSavePath(steamRoot, prefixDir);
            if (!string.IsNullOrEmpty(savePath) && Directory.Exists(savePath)) {
                Logger.Info($"Grim Dawn save path found: {savePath} ({saveSource})");
            }
            return savePath;
        }

        public static List<string> GetGrimFolders() {
            var steamRoot = FindSteamRoot();
            Logger.Info($"Steam installation found: {steamRoot}");
            var libraries = ReadLibraryFolders(steamRoot);
            Logger.Info($"Found {libraries.Count} Steam library location(s)");
            var gameDir = FindGrimDawn(libraries);
            if (gameDir.Count > 0) {
                foreach (var path in gameDir) {
                    Logger.Info($"Grim Dawn installation found: {path}");
                }
            }
            else {
                Logger.Warn("Could not find Grim Dawn in any Steam library");
            }
            return gameDir;
        }

        public static string? GetGrimUserPrefix() {
            var steamRoot = FindSteamRoot();
            Logger.Info($"Steam installation found: {steamRoot}");
            var libraries = ReadLibraryFolders(steamRoot);
            Logger.Info($"Found {libraries.Count} Steam library location(s)");
            var prefixDir = FindPrefix(libraries);
            if (!string.IsNullOrEmpty(prefixDir) && Directory.Exists(prefixDir)) {
                Logger.Info($"Grim Dawn Proton prefix found: {prefixDir}");
            }
            else {
                Logger.Warn("Could not find Grim Dawn Proton prefix");
            }

            var prefixUserDir = FindPrefixUser(prefixDir);
            if (!string.IsNullOrEmpty(prefixUserDir) && Directory.Exists(prefixUserDir)) {
                Logger.Info($"Grim Dawn Proton user prefix found: {prefixUserDir}");
            }
            else {
                Logger.Warn("Could not find Grim Dawn Proton user prefix");
            }
            return prefixUserDir;
        }

        private static string? FindPrefixUser(string? prefixDir) {
            if (!string.IsNullOrEmpty(prefixDir) && Directory.Exists(prefixDir)) {
                var prefixUser = Path.Combine( prefixDir, "drive_c", "users", "steamuser");
                if (Directory.Exists(prefixUser)) {
                    return prefixUser;
                }
            }
            return null;
        }

        private static string FindSteamRoot() {
            foreach (var candidate in SteamRootCandidates) {
                var path = Expand(candidate);
                Logger.Debug($"Checking Steam location: {path}");
                if (Directory.Exists(Path.Combine(path, "steamapps"))) {
                    return path;
                }
            }
            throw new DirectoryNotFoundException("No Steam installation found. Tried: " + string.Join(", ", SteamRootCandidates));
        }

        public static List<string> FindGrimDawn(IEnumerable<string> libraries) {
            var paths = new List<string>();

            foreach (var library in libraries) {
                var path = Path.Combine(library, "steamapps", "common", "Grim Dawn");
                if (File.Exists(Path.Combine(path, "database", "database.arz"))) {
                    paths.Add(path);
                }
            }
            return paths;
        }

        private static string? FindPrefix(IEnumerable<string> libraries) {
            return libraries.Select(l => Path.Combine(l, "steamapps", "compatdata", GrimDawnAppId, "pfx")).FirstOrDefault(Directory.Exists);
        }

        private static List<string> ReadLibraryFolders(string steamRoot) {
            var libraries = new List<string> { steamRoot };
            var vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdfPath)) {
                Logger.Warn($"Steam library config not found: {vdfPath}");
                return libraries;
            }
            foreach (var line in File.ReadLines(vdfPath)) {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Example:
                // "path"      "/mnt/games/SteamLibrary"
                var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries);
                var path = parts.LastOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                if (!Directory.Exists(path)) {
                    Logger.Warn($"Steam library path does not exist: {path}");
                    continue;
                }
                if (!libraries.Contains(path)) {
                    libraries.Add(path);
                    Logger.Debug($"Steam library found: {path}");
                }
            }
            return libraries;
        }

        private static (string?, string?) FindSavePath(string steamRoot, string? prefix) {
            var userdata = Path.Combine(steamRoot, "userdata");
            if (Directory.Exists(userdata)) {
                foreach (var user in Directory.GetDirectories(userdata)) {
                    var candidate = Path.Combine(user, GrimDawnAppId, "remote", "save");
                    if (File.Exists(Path.Combine(candidate, "transfer.gst"))) {
                        return (candidate, $"Steam Cloud userdata (user {Path.GetFileName(user)})");
                    }
                }
            }

            if (!string.IsNullOrEmpty(prefix) && Directory.Exists(prefix)) {
                var docs = Path.Combine(prefix, "drive_c", "users", "steamuser", "Documents", "My Games", "Grim Dawn", "save");
                if (!Directory.Exists(docs)) {
                    docs = Path.Combine(prefix, "drive_c", "users", "steamuser", "Documents", "My Games", "Grim Dawn", "Save");
                }

                if (Directory.Exists(docs)) {
                    var populated = Directory.EnumerateFiles(docs, "transfer.*").Any();
                    return (docs, populated ? "prefix Documents" : "prefix Documents (EMPTY)");
                }
            }
            return (null, null);
        }

        private static string Expand(string path) {
            if (!path.StartsWith('~'))
                return path;

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);
        }
    }
}
