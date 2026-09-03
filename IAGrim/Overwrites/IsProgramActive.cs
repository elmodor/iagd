using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace IAGrim.Overwrites.IsProgramActive;

public static class IsProgramActive
{
    public static bool IsActive()
    {
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        return mainWindow?.IsActive == true;
    }
}
