
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Chat.Ui;

public sealed class MessageView
{
    private readonly FrameView _panel;
    private readonly Label _lines;
    private const string Separator = "────────────────────────────";

    public MessageView(Pos x, Pos y, Dim width, Dim height)
    {
        _panel = new FrameView
        {
            Title = "Messages",
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };

        _panel.VerticalScrollBar.Visible = true;

        _lines = new Label
        {
            Text = "",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(),
        };

        _lines.TextFormatter.WordWrap = true;
        _panel.Add(_lines);

        _panel.SetScheme(Theme.Accent(Theme.Blue));
        _lines.SetScheme(Theme.Readable);
    }

    public View View => _panel;

    public string Title
    {
        get => _panel.Title;
        set => _panel.Title = value;
    }

    public void Append(string message)
    {
        _lines.Text += _lines.Text.Length == 0
            ? message
            : "\n" + Separator + "\n" + message;

        ScrollToBottom();
    }

    public void SetLines(IEnumerable<string> messages)
    {
        _lines.Text = string.Join(
            "\n" + Separator + "\n",
            messages
        );

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        var contentHeight = _lines.TextFormatter
            .FormatAndGetSize()
            .Height;

        _panel.SetContentHeight(contentHeight);

        var viewportHeight = _panel.Viewport.Height;

        _panel.Viewport = _panel.Viewport with
        {
            Y = Math.Max(0, contentHeight - viewportHeight)
        };
    }
}
