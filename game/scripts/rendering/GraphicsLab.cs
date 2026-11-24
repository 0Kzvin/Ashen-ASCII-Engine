using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

public partial class GraphicsLab : Node3D
{
    [Export(PropertyHint.Range, "3,11,0.1")] public float Distance { get; set; } = 4.15f;
    [Export(PropertyHint.Range, "20,70,1")] public float Lens { get; set; } = 42f;
    [Export(PropertyHint.Range, "0,2.8,0.05")] public float Height { get; set; } = 1.63f;
    [Export] public float Azimuth { get; set; } = 14f;

    Knight knight;
    Camera3D camera;
    OmniLight3D key, rim;
    AsciiRenderer renderer;
    ArtProfile profile = new();
    bool rotating = true, attack;
    float yaw = -.22f;
    Label info;

    public override void _Ready()
    {
        AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = Colors.Black,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("#758aad"), AmbientLightEnergy = .16f,
            GlowEnabled = true, GlowIntensity = .28f, GlowHdrThreshold = 1.30f
        }});
        key = Lamp(new(2.3f, 2.1f, -2.24f), "#ffc087", 4.4f);
        rim = Lamp(new(-2.4f, 2.57f, 1.1f), "#85bcff", 4.9f);
        Lamp(new(-.4f, 3.1f, -2.0f), "#82a1b7", .75f);

        var stage = new StaticBody3D { Position = new Vector3(0, -.12f, 0) };
        stage.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new(12, .24f, 12) } });
        AddChild(stage);

        knight = new Knight { Position = new(0, .04f, 0), Rotation = new(0, Mathf.Pi + yaw, 0) };
        AddChild(knight);
        camera = new Camera3D { Current = true, Near = .08f, Far = 25f };
        AddChild(camera);
        Frame();

        var compositor = new CanvasLayer { Layer = 2 };
        AddChild(compositor);
        renderer = new AsciiRenderer();
        compositor.AddChild(renderer);
        renderer.AttachGeometry(camera);
        var ui = new CanvasLayer { Layer = 5 };
        AddChild(ui);
        info = InterfaceStyle.Label("", 18);
        info.Position = new(26, 23);
        info.AddThemeColorOverride("font_shadow_color", Colors.Black);
        info.AddThemeConstantOverride("shadow_offset_y", 2);
        ui.AddChild(info);
        Apply();

        string[] args = OS.GetCmdlineUserArgs();
        int index = Array.IndexOf(args, "--lab-capture");
        if (index >= 0) Capture(index + 1 < args.Length ? args[index + 1] : ProjectSettings.GlobalizePath("user://lab"));
    }

    OmniLight3D Lamp(Vector3 position, string color, float energy)
    {
        var light = new OmniLight3D { Position = position, LightColor = new Color(color), LightEnergy = energy,
            OmniRange = 6, ShadowEnabled = true };
        AddChild(light);
        return light;
    }

    void Frame()
    {
        float azimuth = Mathf.DegToRad(Azimuth);
        camera.Position = new Vector3(Mathf.Sin(azimuth)*Distance, Height, -Mathf.Cos(azimuth)*Distance);
        camera.Fov = Lens;
        camera.LookAt(new Vector3(0, 1.08f, 0));
    }

    void Apply()
    {
        var p=profile;
        renderer.Apply(p.Mode,p.Cell,p.Density,p.Contrast,p.Brightness,p.Saturation,p.Exposure,p.Edges,p.Stability,p.Palette,p.GlyphSet);
        info.Text = $"{GameHud.Modes[p.Mode]}   ·   1–3 Modos   ·   Espacio Girar   ·   Clic Atacar\nRueda Distancia   ·   Q/E Ángulo   ·   L Luz   ·   Esc Volver";
    }

    public override void _PhysicsProcess(double delta)
    {
        if (rotating && !knight.Attacking) yaw += (float)delta * .56f;
        knight.Drive(Vector2.Zero, knight.Position + new Vector3(Mathf.Sin(yaw),0,Mathf.Cos(yaw))*-4, attack, false, delta);
        attack = false;
        float turn = (Input.IsPhysicalKeyPressed(Key.E) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Q) ? 1 : 0);
        if (turn != 0) { Azimuth += turn * 58 * (float)delta; Frame(); }
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex == MouseButton.Left) attack = true;
            if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                Distance = Mathf.Clamp(Distance + (mouse.ButtonIndex == MouseButton.WheelUp ? -.35f : .35f), 3, 11);
                Frame();
            }
        }
        if (input is not InputEventKey { Pressed: true, Echo: false } keyEvent) return;
        switch (keyEvent.Keycode)
        {
            case Key.Key1: profile.Mode = 0; break;
            case Key.Key2: profile.Mode = 3; break;
            case Key.Key3: profile.Mode = AsciiRenderer.AdvancedMode; break;
            case Key.Space: rotating = !rotating; break;
            case Key.L: key.Visible = !key.Visible; break;
            case Key.Escape: GetTree().ChangeSceneToFile("res://scenes/Main.tscn"); return;
            default: return;
        }
        Apply();
    }

    async void Capture(string output)
    {
        try
        {
            Directory.CreateDirectory(output);
            info.Hide();
            rotating = false;
            for (int i=0;i<32;i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            SetPhysicsProcess(false);
            await Shot(output, "laboratorio-3d", 0);
            await Shot(output, "laboratorio-original", 3);
            await Shot(output, "laboratorio-v2", 5);
            profile.Cell = 12;
            profile.Density = .78f;
            await Shot(output, "laboratorio-grueso", 5);
            profile.Cell = 7;
            profile.Density = 1.35f;
            await Shot(output, "laboratorio-denso", 5);
            key.LightEnergy = 1.3f;
            rim.LightEnergy = 8;
            profile = new ArtProfile();
            await Shot(output, "laboratorio-contraluz", 5);
            rotating = true;
            SetPhysicsProcess(true);
            for (int i=0;i<42;i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            attack = true;
            for (int i=0;i<22;i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await Shot(output, "laboratorio-ataque", 5, 1);
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    async Task Shot(string directory, string name, int mode, int frames = 7)
    {
        profile.Mode = mode;
        Apply();
        for (int i=0;i<frames;i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        var result = image.SavePng(Path.Combine(directory, name + ".png"));
        if (result != Error.Ok) throw new IOException($"Captura {name}: {result}");
        GD.Print("PASS capture " + name);
    }
}
