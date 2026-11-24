using Godot;
using System;
using System.Linq;

public partial class Main : Node3D
{
    public Knight Player {get;private set;}
    public Knight Warden {get;private set;}
    public OrbitRig Camera {get;private set;}
    public ArtProfile Profile {get;private set;} = new();
    public Ruins World {get;private set;}
    public AsciiRenderer Renderer {get;private set;}
    public StudioPanel Studio {get;private set;}
    public GameHud Hud {get;private set;}
    public Label Diagnostics {get;private set;}
    public bool StudioOpen => Studio.Visible;
    public bool PausedMenu => menu.Visible;
    Control menu;
    Label cameraButtonLabel;
    Button tourButton;
    int lastAscii=5;
    bool attack,dodge;
    double refresh;
    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        World=new Ruins{Name="Osario",ProcessMode=ProcessModeEnum.Pausable};AddChild(World);
        var sound=new Soundscape{Name="Soundscape",ProcessMode=ProcessModeEnum.Pausable};AddChild(sound);
        sound.SetPortal(new Vector3(0,1,-5));
        Player=Spawn("Caballero",new(-1,.12f,2),false);
        Warden=Spawn("Vigía",new(3.5f,.12f,-1.8f),true);
        Camera=new OrbitRig{Name="Orbita",ProcessMode=ProcessModeEnum.Pausable};AddChild(Camera);
        var fx=new CanvasLayer{Layer=2,Name="ASCII"};AddChild(fx);
        Renderer=new AsciiRenderer();fx.AddChild(Renderer);
        Renderer.AttachGeometry(Camera);
        var ui=new CanvasLayer{Layer=5,Name="Interfaz"};AddChild(ui);
        Hud=new GameHud{Game=this};ui.AddChild(Hud);
        Diagnostics=new Label {Position=new(30,118),Visible=false,MouseFilter=Control.MouseFilterEnum.Ignore};
        Diagnostics.AddThemeFontOverride("font",InterfaceStyle.Body);
        Diagnostics.AddThemeFontSizeOverride("font_size",16);
        Diagnostics.AddThemeStyleboxOverride("normal",InterfaceStyle.Box("#12191bef","#5b6056",12));
        ui.AddChild(Diagnostics);
        Studio=new StudioPanel{Game=this,Visible=false};ui.AddChild(Studio);
        menu=MakeMenu(ui);
        ApplyProfile();
        string[] args=OS.GetCmdlineUserArgs();
        if(args.Contains("--lab") || args.Contains("--lab-capture")) OpenLab();
        else if(args.Contains("--qa")||args.Contains("--capture")||args.Contains("--benchmark")||args.Contains("--film"))
            AddChild(new DemoValidation{Game=this});
    }

    Knight Spawn(string name,Vector3 position,bool enemy)
    {
        var knight=new Knight{Name=name,Position=position,Enemy=enemy,ProcessMode=ProcessModeEnum.Pausable};
        AddChild(knight);return knight;
    }
    public void SetProfile(ArtProfile profile){Profile=profile;ApplyProfile();}
    public void ApplyProfile()
    {
        var p=Profile;p.ClampValues();
        Renderer.Apply(p.Mode,p.Cell,p.Density,p.Contrast,p.Brightness,p.Saturation,p.Exposure,p.Edges,p.Stability,p.Palette,p.GlyphSet);
        World.SetAtmosphere(p.Light,p.Fog,p.Particles,p.Glow);
        Player.SetEffects(p.Particles);Warden.SetEffects(p.Particles);
        GetViewport().Scaling3DScale=p.Scale;
        Hud.QueueRedraw();
    }
    public void Compare()
    {
        if(Profile.Mode==0)Profile.Mode=lastAscii;
        else {lastAscii=Profile.Mode;Profile.Mode=0;}
        ApplyProfile();
        if(StudioOpen)Studio.Rebuild();
    }
    public void ToggleStudio()
    {
        if(PausedMenu)TogglePause();
        Studio.Visible=!Studio.Visible;
        if(StudioOpen)Studio.Rebuild();
        Hud.QueueRedraw();attack=false;dodge=false;
    }
    public void TogglePause()
    {
        menu.Visible=!menu.Visible;
        if(menu.Visible){Studio.Visible=false;tourButton.Text=Camera.Touring?"Terminar recorrido":"Recorrido cinematográfico";}
        GetTree().Paused=menu.Visible;
        Hud.QueueRedraw();attack=false;dodge=false;
    }
    public void Fullscreen()
    {
        var window=GetWindow();
        window.Mode=window.Mode==Window.ModeEnum.Windowed?Window.ModeEnum.Fullscreen:Window.ModeEnum.Windowed;
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch(key.Keycode)
            {
                case Key.Escape: if(StudioOpen)ToggleStudio();else TogglePause();break;
                case Key.F1:ToggleStudio();break;
                case Key.F2:Compare();break;
                case Key.F3:Diagnostics.Visible=!Diagnostics.Visible;refresh=0;break;
                case Key.F11:Fullscreen();break;
                case Key.C when !PausedMenu && !StudioOpen:Camera.ToggleTour();Hud.QueueRedraw();break;
                case Key.Space when !PausedMenu && !StudioOpen:dodge=true;break;
                default:return;
            }
            GetViewport().SetInputAsHandled();
        }
        if(ev is InputEventMouseButton m && m.Pressed && !PausedMenu && !StudioOpen)
        {
            if(m.ButtonIndex==MouseButton.Left)attack=true;
            if(m.ButtonIndex==MouseButton.WheelUp)Camera.ZoomBy(-1.1f);
            if(m.ButtonIndex==MouseButton.WheelDown)Camera.ZoomBy(1.1f);
        }
    }
    static float Axis(Key positive,Key negative) =>
        (Input.IsPhysicalKeyPressed(positive)?1:0)-(Input.IsPhysicalKeyPressed(negative)?1:0);
    public override void _PhysicsProcess(double delta)
    {
        if(PausedMenu)return;
        bool live=!StudioOpen && !Camera.Touring;
        Vector3 direction=live?Camera.GroundRight*Axis(Key.D,Key.A)+Camera.GroundBack*Axis(Key.S,Key.W):Vector3.Zero;
        var aim=live?Camera.AimAtFloor(GetViewport().GetMousePosition(),Player.GlobalPosition):Player.GlobalPosition-Player.GlobalTransform.Basis.Z*3;
        Player.Drive(new Vector2(direction.X,direction.Z).LimitLength(),aim,live&&attack,live&&dodge,delta);
        attack=false;dodge=false;
        Camera.Follow(Player.GlobalPosition,live?Axis(Key.E,Key.Q):0,delta);
    }
    public override void _Process(double delta)
    {
        refresh-=delta;
        if(refresh>0)return;
        refresh=.2;
        Hud.QueueRedraw();
        if(!Diagnostics.Visible)return;
        var size=DisplayServer.WindowGetSize();
        float fps=(float)Engine.GetFramesPerSecond();
        int cells=Mathf.CeilToInt(size.X/Profile.Cell)*Mathf.CeilToInt(size.Y/Mathf.Round(Profile.Cell*1.36f));
        double mem=Performance.GetMonitor(Performance.Monitor.MemoryStatic)/1048576;
        Diagnostics.Text=$"{fps:0} FPS    ·    {1000/Mathf.Max(1,fps):0.0} ms\n{size.X} × {size.Y}    ·    Escala 3D {Profile.Scale:P0}\nCelda {Profile.Cell:0} px    ·    ≈ {cells:N0} celdas\n{RenderingServer.GetCurrentRenderingMethod()}    ·    {RenderingServer.GetVideoAdapterName()}\nMemoria estática del motor    {mem:N0} MiB";
    }

    Control MakeMenu(CanvasLayer ui)
    {
        var veil=new ColorRect {Color=new Color(.02f,.035f,.04f,.86f),Visible=false,Theme=InterfaceStyle.Create()};
        ui.AddChild(veil);veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var center=new CenterContainer();veil.AddChild(center);center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var box=new VBoxContainer{CustomMinimumSize=new Vector2(370,0)};center.AddChild(box);
        var zone=InterfaceStyle.Label("EL OSARIO DE LA VIGILIA",14);zone.Modulate=InterfaceStyle.Gold;box.AddChild(zone);
        box.AddChild(InterfaceStyle.Label("Pausa",54,true));
        box.AddChild(new HSeparator());
        void Button(string text,Action action)
        {
            var b=new Button{Text=text};b.Pressed+=action;box.AddChild(b);
            if(text.StartsWith("Recorrido"))tourButton=b;
        }
        Button("Continuar",TogglePause);Button("ASCII Studio",ToggleStudio);
        Button("Recorrido cinematográfico",()=>{TogglePause();Camera.ToggleTour();});
        Button("Laboratorio gráfico",OpenLab);
        Button("Reiniciar escena",Restart);Button("Salir al escritorio",()=>ExitTo());
        cameraButtonLabel=InterfaceStyle.Label("Q / E   Girar     ·     Rueda   Zoom     ·     C   Recorrido\nF2   Comparar     ·     F3   Rendimiento     ·     F11   Pantalla",15);
        cameraButtonLabel.Modulate=InterfaceStyle.Muted;box.AddChild(cameraButtonLabel);
        return veil;
    }
    public void Restart() => ExitTo("res://scenes/Main.tscn");
    private void OpenLab() => ExitTo("res://scenes/GraphicsLab.tscn");

    public async void ExitTo(string scene = null, int code = 0)
    {
        if (exiting) return;
        exiting = true;
        Soundscape.Current?.Stop();
        GetTree().Paused = false;
        await ToSignal(GetTree().CreateTimer(.08), SceneTreeTimer.SignalName.Timeout);
        if (scene == null) GetTree().Quit(code);
        else GetTree().ChangeSceneToFile(scene);
    }

    bool exiting;
}
