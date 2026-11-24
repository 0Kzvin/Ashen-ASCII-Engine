using Godot;

public static class InterfaceStyle
{
    public static readonly Color Ivory = new("#e7dfcf"), Gold = new("#c9a370"), Muted = new("#a3b0ac");
    public static readonly Font Body = GD.Load<Font>("res://assets/ui/Barlow.ttf");
    public static readonly Font Title = GD.Load<Font>("res://assets/ui/Cormorant.ttf");
    public static StyleBoxFlat Box(string color, string border="#42463e", int padding=8)
    {
        var box=new StyleBoxFlat {BgColor=new Color(color),BorderColor=new Color(border)};
        box.SetBorderWidthAll(1);
        foreach (Side side in new[]{Side.Left,Side.Top,Side.Right,Side.Bottom}) box.SetContentMargin(side,padding);
        return box;
    }
    public static Theme Create()
    {
        var theme=new Theme {DefaultFont=Body,DefaultFontSize=16};
        foreach(string type in new[]{"Button","OptionButton"})
        {
            theme.SetStylebox("normal",type,Box("#222925"));
            theme.SetStylebox("hover",type,Box("#343b31","#ba9b6e"));
            theme.SetStylebox("pressed",type,Box("#42483b","#c9a370"));
            theme.SetColor("font_color",type,Ivory);
        }
        theme.SetStylebox("panel","PopupMenu",Box("#171e1d"));
        theme.SetColor("font_color","Label",Ivory);
        return theme;
    }
    public static Label Label(string caption,int size=16, bool title=false)
    {
        var label=new Label {Text=caption, MouseFilter=Control.MouseFilterEnum.Ignore};
        label.AddThemeFontOverride("font",title?Title:Body);
        label.AddThemeFontSizeOverride("font_size",size);
        return label;
    }
}
