using Avalonia.Controls;

namespace IAGrim.Overwrites.MessageBox;

public partial class MessageBoxWindow : Window
{
    private MessageBoxResult _result = MessageBoxResult.None;

    public MessageBoxWindow()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            if (_result == MessageBoxResult.None)
            {
                _result = MessageBoxResult.Cancel;
            }
        };
    }

    public void Configure(string message, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        MessageText.Text = message;
        IconText.Text = GetIcon(icon);
        AddButtons(buttons);
    }

    private static string GetIcon(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Information => "ℹ",
        MessageBoxIcon.Warning => "⚠",
        MessageBoxIcon.Error => "✖",
        _ => string.Empty
    };

    private void AddButtons(MessageBoxButtons buttons)
    {
        ButtonPanel.Children.Clear();
        switch (buttons)
        {
            case MessageBoxButtons.OK:
                AddButton("OK", MessageBoxResult.OK, isDefault: true);
                break;

            case MessageBoxButtons.OKCancel:
                AddButton("OK", MessageBoxResult.OK, isDefault: true);
                AddButton("Cancel", MessageBoxResult.Cancel, isCancel: true);
                break;

            case MessageBoxButtons.YesNo:
                AddButton("Yes", MessageBoxResult.Yes, isDefault: true);
                AddButton("No", MessageBoxResult.No, isCancel: true);
                break;

            case MessageBoxButtons.YesNoCancel:
                AddButton("Yes", MessageBoxResult.Yes, isDefault: true);
                AddButton("No", MessageBoxResult.No);
                AddButton("Cancel", MessageBoxResult.Cancel, isCancel: true);
                break;
        }
    }

    private void AddButton(string text, MessageBoxResult result, bool isDefault = false, bool isCancel = false)
    {
        var button = new Button
        {
            Content = text,
            IsDefault = isDefault,
            IsCancel = isCancel,
            MinWidth = 80
        };
        button.Click += (_, _) =>
        {
            _result = result;
            Close(_result);
        };
        ButtonPanel.Children.Add(button);
    }
}
