namespace IAGrim.Overwrites.LinuxConfig;

public static class LinuxConfig
{
    public static string ConfigDirectory
    {
        get {
            var xdgConfigDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            var baseConfigDirectory = !string.IsNullOrWhiteSpace(xdgConfigDir) ? xdgConfigDir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            var iagdConfigDir = Path.Combine(baseConfigDirectory, "IAGrim");
            Directory.CreateDirectory(iagdConfigDir);

            return iagdConfigDir;
        }
    }

    public static string DataDirectory
    {
        get {
            var xdgDataDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var baseDataDirectory = !string.IsNullOrWhiteSpace(xdgDataDir) ? xdgDataDir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            var iagdDataDir = Path.Combine(baseDataDirectory, "IAGrim");
            Directory.CreateDirectory(iagdDataDir);

            return iagdDataDir;
        }
    }
}
