using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Chat.Ui;

public static class Theme
{
    public static readonly Color Base = new("#1e1e2e");
    public static readonly Color Surface = new("#313244");
    public static readonly Color Text = new("#cdd6f4");
    public static readonly Color Blue = new("#89b4fa");
    public static readonly Color Green = new("#a6e3a1");
    public static readonly Color Mauve = new("#cba6f7");
    public static readonly Color Peach = new("#fab387");

    public static Scheme Readable => new()
    {
        Normal = new Attribute(Text, Base),
    };

    public static Scheme Input => new()
    {
        Normal = new Attribute(Text, Surface),
        Focus = new Attribute(Text, Surface),
    };

    public static Scheme Accent(Color color) => new()
    {
        Normal = new Attribute(color, Base),
        HotNormal = new Attribute(color, Base),
        Focus = new Attribute(Base, color),
        HotFocus = new Attribute(Base, color),
    };
}
