using Godot;
using static InterfaceStyle;

public partial class GameHud : Control
{
    public Main Game;
    public static readonly string[] Modes={"Escena 3D","Monocromo","ASCII a color","ASCII original","Diagnóstico","ASCII cinematográfico V2"};
    GradientTexture2D wash;
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter=MouseFilterEnum.Ignore;
        wash=new GradientTexture2D {Width=256,Height=4,FillFrom=Vector2.Zero,FillTo=Vector2.Right,
            Gradient=new Gradient {Colors=new[]{new Color(.025f,.033f,.035f,.78f),new Color(.025f,.033f,.035f,0)}}};
    }
    void Text(Vector2 at,string value,int size,Color color,bool display=false)
    {
        Font font=display?Title:Body;
        DrawString(font,at+new Vector2(1,2),value,HorizontalAlignment.Left,-1,size,new Color(0,0,0,.65f));
        DrawString(font,at,value,HorizontalAlignment.Left,-1,size,color);
    }
    void Line(Vector2 a,Vector2 b,Color c,float width=1) => DrawLine(a,b,c,width,true);
    void Sigil(Vector2 p,float s)
    {
        Vector2[] points={new(0,-1),new(.63f,.1f),new(0,.95f),new(-.63f,.1f),new(0,-1)};
        for(int i=1;i<points.Length;i++) Line(p+points[i-1]*s,p+points[i]*s,Gold,1.6f);
        Line(p+new Vector2(0,-.65f)*s,p+new Vector2(0,.52f)*s,Gold,2);
        Line(p+new Vector2(-.28f,.19f)*s,p+new Vector2(.28f,.19f)*s,Gold,1.5f);
    }
    public override void _Draw()
    {
        if(Game?.Player==null || Game.PausedMenu) return;
        float w=Size.X,h=Size.Y;
        DrawTextureRect(wash,new Rect2(0,12,340,92),false);
        Sigil(new Vector2(41,54),25);
        Text(new Vector2(75,58),"A S H E N",37,Ivory,true);
        Text(new Vector2(76,82),"EL OSARIO DE LA VIGILIA",13,Gold);
        if(!Game.StudioOpen)
        {
            string mode=Modes[Game.Profile.Mode];
            float tw=Body.GetStringSize(mode,fontSize:15).X;
            Text(new Vector2(w-100-tw,46),mode,15,Ivory);
            DrawRect(new Rect2(w-84,28,52,24),new Color("#121b1cec"));
            DrawRect(new Rect2(w-84,28,52,24),new Color("#797464"),false);
            Text(new Vector2(w-65,46),"F1",14,Gold);
        }
        if(Game.Camera.Touring)
        {
            Text(new Vector2(32,h-32),"RECORRIDO   /   C para volver",15,Gold);
            return;
        }
        DrawTextureRect(wash,new Rect2(0,h-95,350,75),false);
        Text(new Vector2(32,h-60),"CABALLERO DE ASCIA",13,Gold);
        float health=(float)Game.Player.Health/Game.Player.MaxHealth;
        DrawRect(new Rect2(32,h-47,174,6),new Color("#362a2b"));
        DrawRect(new Rect2(32,h-47,174*health,6),new Color("#b9503c"));
        Line(new Vector2(32,h-48),new Vector2(32+174*health,h-48),new Color("#ebad7a"));
        Text(new Vector2(217,h-39),$"{Game.Player.Health} / {Game.Player.MaxHealth}",13,Ivory);
        if(!Game.StudioOpen)
        {
            string help="WASD   Mover      ·      Clic   Atacar      ·      Espacio   Esquivar      ·      Esc   Menú";
            Text(new Vector2(w-32-Body.GetStringSize(help,fontSize:14).X,h-34),help,14,Ivory);
        }
    }
}
