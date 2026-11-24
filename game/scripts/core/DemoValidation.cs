using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// Explicit QA entry points only. Normal gameplay never reads a viewport back to CPU.
public partial class DemoValidation : Node
{
    public Main Game;
    string[] args;
    string output;
    bool restarting;
    public override async void _Ready()
    {
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        try
        {
            args=OS.GetCmdlineUserArgs();
            output=Opt("--out",ProjectSettings.GlobalizePath("user://qa"));
            Directory.CreateDirectory(output);
            if(args.Contains("--qa"))await Checks();
            if(restarting)return;
            if(args.Contains("--capture"))await Gallery();
            if(args.Contains("--benchmark"))await Benchmark();
            if(args.Contains("--film")) await Film();
            if(IsInsideTree())Game.ExitTo();
        }
        catch(Exception e){GD.PushError("ASHEN QA: "+e);Game.ExitTo(code:1);}
    }
    string Opt(string name,string fallback)
    {int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
    async Task Frames(int count)
    {for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    async Task Physics(int count)
    {for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
    static void Check(bool condition,string name)
    {if(!condition)throw new InvalidOperationException(name);GD.Print("PASS "+name);}
    async Task Checks()
    {
        if(GetTree().HasMeta("restart_check"))
        {Check(Game.Player.Health==Game.Player.MaxHealth&&!Game.PausedMenu,"scene restarted");return;}
        await Physics(12);
        Check(Game.Player.IsOnFloor(),"player on collision floor");
        foreach(string file in new[]{"res://assets/fonts/glyph_atlas.png","res://shaders/ascii_composite.gdshader","res://assets/ui/Cormorant.ttf","res://scripts/audio/samples/impact.wav"})
            Check(ResourceLoader.Exists(file),file);
        var start=Game.Player.Position;
        Game.SetPhysicsProcess(false);
        for(int i=0;i<18;i++){Game.Player.Drive(Vector2.Right,start+Vector3.Right*4,false,false,1/60.0);await Physics(1);}
        Check(Game.Player.Position.X>start.X+.3f,"movement");
        Game.Player.Drive(Vector2.Zero,Game.Warden.Position,false,true,1/60.0);
        Check(Game.Player.Dodging,"dodge");
        await Physics(1);
        for(int i=0;i<65;i++){Game.Player.Drive(Vector2.Zero,Game.Warden.Position,false,false,1/60.0);await Physics(1);}
        Game.Warden.GlobalPosition=Game.Player.GlobalPosition+Vector3.Right*1.1f;
        var health=Game.Warden.Health;
        for(int i=0;i<34;i++){Game.Player.Drive(Vector2.Zero,Game.Warden.Position,i==0,false,1/60.0);await Physics(1);}
        Check(Game.Warden.Health<health,"attack and impact");
        Game.SetPhysicsProcess(true);
        Game.ToggleStudio();Check(Game.StudioOpen,"studio opened");Game.ToggleStudio();
        foreach(int mode in Enumerable.Range(0,7))
        {Game.Profile.Mode=mode;Game.ApplyProfile();Check(Game.Renderer.Visible==(mode!=0),"render mode "+mode);}
        Game.Compare();Game.Compare();Check(Game.Profile.Mode==6,"comparison restores mode");
        foreach(int quality in Enumerable.Range(0,4))
        {Game.SetProfile(ArtProfile.Preset(quality));Check(Math.Abs(Game.GetViewport().Scaling3DScale-Game.Profile.Scale)<.01,"quality "+quality);}
        var p=Game.Profile;p.Cell=13;p.Palette=2;p.GlyphSet=5;
        Check(p.Save(913)=="Perfil guardado","save test slot");
        var restored=ArtProfile.Load(913);
        Check(restored!=null&&restored.Cell==13&&restored.Palette==2&&restored.GlyphSet==5,"profile roundtrip");
        File.Delete(ProjectSettings.GlobalizePath("user://profile-913.json"));
        Game.TogglePause();Check(GetTree().Paused,"pause");Game.TogglePause();Check(!GetTree().Paused,"resume");
        if(DisplayServer.GetName()!="headless")
        {
            var w=Game.GetWindow();w.Mode=Window.ModeEnum.Windowed;w.Size=new Vector2I(1152,648);await Frames(6);
            Check(w.Size==new Vector2I(1152,648),"window resize");
            Game.Fullscreen();await Frames(5);Check(w.Mode==Window.ModeEnum.Fullscreen,"fullscreen");Game.Fullscreen();
        }
        GetTree().SetMeta("restart_check",true);
        restarting=true;
        Game.CallDeferred(nameof(Main.Restart));

    }
    async Task Shot(string name,int wait=8)
    {
        await Frames(wait);
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        using var image=GetViewport().GetTexture().GetImage();
        var result=image.SavePng(Path.Combine(output,name+".png"));
        Check(result==Error.Ok,"capture "+name);
    }
    async Task Gallery()
    {
        await Frames(60);
        Game.Warden.SetPhysicsProcess(false);
        Game.SetPhysicsProcess(false);
        Transform3D galleryStart = Game.Camera.GlobalTransform;
        Game.Warden.GlobalPosition = new Vector3(2.9f, .02f, -.85f);
        Game.World.SetProcess(false);
        foreach(var (name,mode) in new[]{("01-ruinas",0),("02-monocromo",1),("03-ceniza",3),("04-color",2),("05-diagnostico",4),("12-v2",5)})
        {Game.Profile.Mode=mode;Game.ApplyProfile();await Shot(name);}
        await CompareView("osario", Game.Camera.GlobalPosition, Game.Camera.GlobalPosition - Game.Camera.GlobalBasis.Z * 8);
        await CompareView("antorcha", new(-2.42f, 2.30f, -0.25f), new(-3.1f, 1.96f, -2.95f));
        await CompareView("portal", new(0.1f, 2.42f, .66f), new(0, 1.91f, -5.40f));
        Game.World.SetProcess(true);
        await Frames(1);
        Game.Camera.GlobalTransform = galleryStart;
        Game.Profile.Mode=5;Game.Profile.Palette=1;Game.Profile.Cell=11;Game.ApplyProfile();await Shot("06-lunar");
        Game.Profile.Palette=2;Game.Profile.Cell=6;Game.ApplyProfile();await Shot("07-cobre");
        Game.SetProfile(ArtProfile.Preset(3));Game.ToggleStudio();await Shot("08-studio");Game.ToggleStudio();
        Game.SetPhysicsProcess(false);
        for(int i=0;i<14;i++){Game.Player.Drive(Vector2.Right,Game.Player.Position+Vector3.Right*4,false,false,1/60.0);await Physics(1);}
        await Shot("09-movimiento",1);
        for(int i=0;i<20;i++){Game.Player.Drive(Vector2.Zero,Game.Player.Position+Vector3.Right*4,i==0,false,1/60.0);await Physics(1);}
        await Shot("10-ataque",7);
        Game.SetPhysicsProcess(true);Game.Camera.ToggleTour();await Shot("11-recorrido",110);
    }
    async Task CompareView(string name, Vector3 eye, Vector3 target)
    {
        Game.Camera.GlobalPosition = eye;
        Game.Camera.LookAt(target);
        Game.SetProfile(new ArtProfile());
        await Shot(name + "-v2");
        Game.SetProfile(ArtProfile.Artistic());
        await Shot(name + "-v3");
    }

    async Task Film()
    {
        int mode = int.Parse(Opt("--mode","6"));
        var profile = mode == 6 ? ArtProfile.Artistic() : new ArtProfile { Mode = mode };
        Game.SetProfile(profile);
        Game.Hud.Hide();
        Game.SetPhysicsProcess(false);
        Game.Warden.SetPhysicsProcess(false);
        Game.Warden.Hide();
        var rate = Math.Max(30, Engine.PhysicsTicksPerSecond);
        Game.Player.GlobalPosition = new Vector3(-.85f, .015f, .55f);
        var refuge = new Vector3(-.85f, 1.19f, .55f);
        for (int i=0; i<rate*27; i++)
        {
            await Physics(1);
            float t = i/(float)rate;
            Vector3 target, eye;
            if (t < 6)
            {
                float a = .47f - t*.047f;
                target = new(-.25f, 2.4f, -1.75f);
                eye = target + new Vector3(Mathf.Sin(a)*13, 6.2f, Mathf.Cos(a)*13);
            }
            else if (t < 14)
            {
                float s = (t-6)/8;
                float a = Mathf.Lerp(-2.76f, -3.62f, s);
                target = refuge + new Vector3(0,.10f,0);
                eye = target + new Vector3(Mathf.Sin(a)*4.55f,.39f+1.05f*s,Mathf.Cos(a)*4.55f);
            }
            else if (t < 20)
            {
                float s = (t-14)/6;
                target = new(-3.10f, 2.04f, -2.95f);
                eye = target + new Vector3(.94f-s*1.02f,.26f,2.90f-s*.47f);
            }
            else
            {
                float s = (t-20)/7;
                target = new(0,2.06f,-5.38f);
                eye = target + new Vector3(.95f-s*1.53f, .88f-s*.32f,7.0f-s*1.5f);
            }
            Game.Camera.GlobalPosition = eye;
            Game.Camera.LookAt(target);
            Vector3 aim = t < 14 ? new Vector3(-1.8f,0,-4.8f) : new Vector3(-2.8f,0,-2.7f);
            Game.Player.Drive(Vector2.Zero, aim, i==rate*11, false, 1.0/rate);
        }
    }

    async Task Benchmark()
    {
        int quality=int.Parse(Opt("--quality","3"));
        Game.SetProfile(ArtProfile.Preset(quality));
        Game.Profile.Mode=int.Parse(Opt("--mode","6"));
        Game.HybridInk = !args.Contains("--screen-only");
        if (Game.Profile.Mode==5) Game.Profile.Cell = 9;
        Game.ApplyProfile();
        Game.Camera.ToggleTour();
        string result = await FrameBenchmark.Sample(this, $"quality={quality} mode={Game.Profile.Mode} hybrid={Game.HybridInk}", 8);
        GD.Print(result);File.WriteAllText(Path.Combine(output,$"benchmark-{quality}-{Game.Profile.Mode}{(Game.HybridInk ? "" : "-pantalla")}.txt"),result);
    }
}
