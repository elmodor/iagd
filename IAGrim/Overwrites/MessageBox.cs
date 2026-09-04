using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace IAGrim.Overwrites.MessageBox;

public enum MessageBoxButtons
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

public enum MessageBoxIcon
{
    None,
    Information,
    Warning,
    Error
}

public enum MessageBoxResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

public static class MessageBox
{
    public static Task<MessageBoxResult> Show(string message, string title = "IAGD", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowCore(message, title, buttons, icon);
        }
        return Dispatcher.UIThread.InvokeAsync(() => ShowCore(message, title, buttons, icon));
    }

    private static async Task<MessageBoxResult> ShowCore(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        var dialog = new MessageBoxWindow
        {
            Title = title
        };
        dialog.Configure(message, buttons, icon);
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner != null)
        {
            return await dialog.ShowDialog<MessageBoxResult>(owner);
        }
        dialog.Show();
        return MessageBoxResult.None;
    }
}
