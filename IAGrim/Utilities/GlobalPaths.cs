using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IAGrim.Overwrites.LinuxConfig;

namespace IAGrim.Utilities {
    internal static class GlobalPaths {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GlobalPaths));
        private static string? _grimDawnWineUserProfilePath;
        public static bool HasGrimDawnWineUserProfilePath => !string.IsNullOrWhiteSpace(_grimDawnWineUserProfilePath);

        public static string GrimDawnWineUserProfilePath {
            get {
                if (string.IsNullOrWhiteSpace(_grimDawnWineUserProfilePath)) {
                    throw new InvalidOperationException("Grim Dawn Wine user profile path has not been configured.");
                }
                return _grimDawnWineUserProfilePath;
            }
            set {
                var grimDawnWineUserProfilePath = value?.Trim();
                if (string.IsNullOrWhiteSpace(grimDawnWineUserProfilePath)) {
                    Logger.Warn("Wine user profile path is empty");
                    throw new ArgumentException($"The selected wine user profile path is empty");
                }
                if (!Directory.Exists(grimDawnWineUserProfilePath)) {
                    Logger.Warn($"Wine user profile path does not exist: {grimDawnWineUserProfilePath}");
                    throw new ArgumentException($"The selected wine user profile path does not exist:\n{grimDawnWineUserProfilePath}");
                }
                var appData = Path.Combine(grimDawnWineUserProfilePath, "AppData");
                var grimDawnDocs = Path.Combine(grimDawnWineUserProfilePath, "Documents", "My Games", "Grim Dawn");
                var savePath = Directory.Exists(Path.Combine(grimDawnDocs, "save")) ? Path.Combine(grimDawnDocs, "save") : Directory.Exists(Path.Combine(grimDawnDocs, "Save")) ? Path.Combine(grimDawnDocs, "Save") : null;
                if (!Directory.Exists(appData) || savePath == null) {
                    Logger.Warn($"Invalid Grim Dawn wine user profile path: {grimDawnWineUserProfilePath}. AppData exists: {Directory.Exists(appData)}, save exists: {Directory.Exists(savePath)}");
                    throw new ArgumentException($"The selected folder does not appear to be a Grim Dawn wine user profile.\nThe following folders must exist:\n{appData}\n{savePath}");
                }
                _grimDawnWineUserProfilePath = Path.GetFullPath(grimDawnWineUserProfilePath);
            }
        }

        private static string LocalAppdata {
            get {
                var appData = Path.Combine(GrimDawnWineUserProfilePath, "AppData");

                var local = Path.Combine(appData, "Local");
                if (Directory.Exists(local))
                    return local;

                var localLower = Path.Combine(appData, "local");
                if (Directory.Exists(localLower))
                    return localLower;

                // Default to the normal Wine/Windows spelling.
                return local;
            }
        }

        public static string ItemsHtmlFile => Path.Combine(StorageFolder, "index.html");

        public static string BackupLocation {
            get {
                string path = Path.Combine(LinuxConfig.DataDirectory, "backup");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvLocation {
            get {
                string path = Path.Combine(CoreFolder, "itemqueue");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvReplicaWriteLocation {
            get {
                string path = Path.Combine(CoreFolder, "replica", "from_ia");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvReplicaReadLocation {
            get {
                string path = Path.Combine(CoreFolder, "replica", "to_ia");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvReplicaDumpLocation {
            get {
                string path = Path.Combine(CoreFolder, "replica", "to_ia", "deleted");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvLocationIngoing {
            get {
                string path = Path.Combine(CsvLocation, "ingoing");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvLocationOutgoing {
            get {
                string path = Path.Combine(CsvLocation, "outgoing");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvLocationIngoingDeleted {
            get {
                string path = Path.Combine(CsvLocation, "ingoing", "deleted");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        

        public static string DebugLocation{
            get {
                string path = Path.Combine(CoreFolder, "debug");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CsvLocationOutgoingDeleted {
            get {
                string path = Path.Combine(CsvLocation, "deleted");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CharacterBackupLocation {
            get {
                string path = Path.Combine(BackupLocation, "characters");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string LinuxHack {
            get {
                string path = Path.Combine(CoreFolder, "linuxhack");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string? DownloadsFolder {
            get {
                return Path.GetTempPath();
            }
        }

        public static string SavePath {
            get {
                var grimDawnDocs = Path.Combine(GrimDawnWineUserProfilePath, "Documents", "My Games", "Grim Dawn");
                var p = Directory.Exists(Path.Combine(grimDawnDocs, "save")) ? Path.Combine(grimDawnDocs, "save") : Path.Combine(grimDawnDocs, "Save");
                Directory.CreateDirectory(p);
                return p;
            }
        }


        public static string UserdataFolder {
            get {
                string path = Path.Combine(LinuxConfig.DataDirectory, "data");
                Directory.CreateDirectory(path);

                return path;
            }
        }

        public static string StorageFolder {
            get {
                string
                    path = Path.Combine(LinuxConfig.DataDirectory, "resources")
                        .Replace("#",
                            ""); // Some brilliant people have hashtags in their windows usernames..  That works poorly when opening HTML files with a # in the path.
                Directory.CreateDirectory(path);

                return path;
            }
        }

#if DEBUG
        public static string SettingsFile => Path.Combine(LinuxConfig.ConfigDirectory, "settings-debug.json").Replace("#", "");
#else
        public static string SettingsFile => Path.Combine(LinuxConfig.ConfigDirectory, "settings.json").Replace("#", "");
#endif

        public static string CoreFolder {
            get {
                var path = Path.Combine(LocalAppdata, "EvilSoft", "IAGD");
                Directory.CreateDirectory(path);
                return path;
            }
        }


#if DEBUG
        public static string USERDATA_FILE => "userdata-test.db";
#else
        public static string USERDATA_FILE => "userdata.db";
#endif
    }
}
