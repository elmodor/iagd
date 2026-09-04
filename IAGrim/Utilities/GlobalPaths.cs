using IAGrim.Parsers.Arz;
using IAGrim.Utilities.HelperClasses;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static IAGrim.Utilities.HelperClasses.GDTransferFile;
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

        // TODO
        public static string? DownloadsFolder {
            get {
                return LinuxConfig.DataDirectory;
                // Guid DownloadsFolderGuid = new Guid("{374DE290-123F-4565-9164-39C4925E467B}");
                // try {
                //     return SHGetKnownFolderPath(DownloadsFolderGuid, 0, IntPtr.Zero);
                // }
                // catch (Exception ex) {
                //     Logger.Warn(ex);
                //     return null;
                // }
            }
        }


        public static bool IsHardcore(string filename) {
            return filename.EndsWith(".gsh") || filename.EndsWith(".csh") || filename.EndsWith(".bsh");
        }

        /// <summary>
        /// Fetches the "downgrade type" of the transfer file.
        /// Transfer files ending with .cst/.csh have disabled the FG expansion.
        /// Transfer files ending with .bst/.bsh have disabled the AoM and FG expansions.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static DowngradeType GetDowngradeType(string filename) {
            var withoutLastLetter = filename.Substring(0, filename.Length - 1);
            if (withoutLastLetter.EndsWith(".cs"))
                return DowngradeType.AoM;
            else if (withoutLastLetter.EndsWith(".bs"))
                return DowngradeType.NoExpansions;
            else
                return DowngradeType.None;
        }


        public static string SavePath {
            get {
                var grimDawnDocs = Path.Combine(GrimDawnWineUserProfilePath, "Documents", "My Games", "Grim Dawn");
                var p = Directory.Exists(Path.Combine(grimDawnDocs, "save")) ? Path.Combine(grimDawnDocs, "save") : Path.Combine(grimDawnDocs, "Save");
                Directory.CreateDirectory(p);
                return p;
            }
        }

        /// <summary>
        /// Map of [mod][transfer file]
        /// </summary>
        public static List<GDTransferFile> GetTransferFiles(bool includeDowngradeFiles) {
            var transferFilesCache = new List<GDTransferFile>();
            HashSet<string> parsedFiles = new HashSet<string>();
            string documents = SavePath;

            var transferFilenames = new string[] {
                "transfer.gst", // Softcore
                "transfer.gsh", // Hardcore
            };

            if (includeDowngradeFiles) {
                transferFilenames = new string[] {
                    "transfer.gst", // Softcore
                    "transfer.gsh", // Hardcore
                    "transfer.bst", // Softcore Vanilla FG/AOM disabled (Vanilla only, owns expansions)
                    "transfer.cst", // Softcore FG disabled (Vanilla+AOM) 
                    "transfer.csh", // Hardcore FG disabled (Vanilla+AOM) 
                    "transfer.dsh", // Hardcore Asterkarn disabled?
                    "transfer.dst", // Softcore Asterkarn disabled?
                    "transfer.bsh" // Hardcore FG/AOM disabled (Vanilla only, owns expansions)
                };
            }

            if (!Directory.Exists(documents)) {
                Logger.Warn($"Could not locate the folder \"{documents}\"");
                return transferFilesCache;
            }


            // Generate a list of the interesting files
            List<string> files = new List<string>();
            // transfer.bst / transfer.cst / transfer.csh
            foreach (string filename in transferFilenames) {
                string vanilla = Path.Combine(documents, filename);
                if (File.Exists(vanilla) && !parsedFiles.Contains(vanilla)) {
                    files.Add(vanilla);
                }


                foreach (var possibleMod in Directory.GetDirectories(documents)) {
                    string mod = Path.Combine(possibleMod, filename);
                    if (File.Exists(mod) && !parsedFiles.Contains(mod)) {
                        files.Add(mod);
                    }
                }
            }


            foreach (string potential in files) {
                if (TransferStashService.TryGetModLabel(potential, out var mod)) {
                    parsedFiles.Add(potential);
                    var lastAccess = File.GetLastWriteTime(potential);
                    transferFilesCache.Add(new GDTransferFile {
                        Filename = potential,
                        Mod = mod,
                        IsHardcore = IsHardcore(potential),
                        LastAccess = lastAccess,
                        Downgrade = GetDowngradeType(potential)
                    });
                }
            }

            if (transferFilesCache.Count == 0) {
                Logger.Debug($"No stash files detected in {documents}");
            }


            return transferFilesCache;
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
