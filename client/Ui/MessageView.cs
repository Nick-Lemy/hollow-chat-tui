using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Chat.Ui;

public sealed class MessageView
{
    private readonly FrameView _panel;
    private readonly Label _lines;
    private int _lineCount;

    public MessageView(Pos x, Pos y, Dim width, Dim height)
    {
        _panel = new FrameView
        {
            Title = "Messages",
            X = x, Y = y,
            Width = width,
            Height = height,
        };
        _panel.VerticalScrollBar.Visible = true;

        _lines = new Label
        {
            Text = "",
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(),
        };
        _panel.Add(_lines);

        _panel.SetScheme(Theme.Accent(Theme.Blue));
        _lines.SetScheme(Theme.Readable);
    }

    public View View => _panel;

    public void Append(string line)
    {
        _lines.Text += _lines.Text.Length == 0 ? line : "\n" + line;
        _lineCount++;

        _panel.SetContentHeight(_lineCount);
        var viewportHeight = _panel.Viewport.Height;
        _panel.Viewport = _panel.Viewport with { Y = Math.Max(0, _lineCount - viewportHeight) };
    }
}
