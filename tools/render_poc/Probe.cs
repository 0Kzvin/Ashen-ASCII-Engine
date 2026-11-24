using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

/// <summary>Render benchmark for seven material swatches under fixed lighting.</summary>
public partial class Probe : Node3D
{
    private const int WarmupFrames = 48;
    private const int SampleFrames = 180;
    private readonly AsciiRenderer _overlay = new();

    public override async void _Ready()
    {
        try
        {
            BuildScene();
            await MeasureModes();
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr(error);
            GetTree().Quit(1);
        }
    }

    private void BuildScene()
    {
        var sky = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color("#161e2b"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.48f
        };
        AddChild(new WorldEnvironment { Environment = sky });
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-45, -28, 0),
            LightEnergy = 1.4f,
            ShadowEnabled = true
        });
        var camera = new Camera3D { Position = new Vector3(0, 5, 16), Fov = 43 };
        AddChild(camera);
        camera.LookAt(new Vector3(0, 2.5f, 0));
        var stone = new Color("#405165");
        Model(new BoxMesh { Size = new Vector3(48, 0.24f, 35) }, Vector3.Down * 0.15f, stone);
        string[] colors = { "#9e153a", "#7442dd", "#677380", "#497f3a", "#22659d", "#aaaaaa", "#f28919" };
        for (int i = 0; i < colors.Length; i++)
        {
            float x = (i - 3) * 2.45f;
            var swatch = new Color(colors[i]);
            Model(new BoxMesh { Size = new Vector3(2.2f, 0.34f, 2.4f) }, new Vector3(x, 0.17f, 0), stone);
            Model(new CylinderMesh { TopRadius = 0.64f, BottomRadius = 0.94f, Height = 2.5f, RadialSegments = 8 },
                new Vector3(x, 1.55f, 0), swatch);
            Model(new TorusMesh { InnerRadius = 0.60f, OuterRadius = 0.98f }, new Vector3(x, 2.9f, 0), swatch);
            Model(new SphereMesh { Radius = 0.73f, Height = 1.46f }, new Vector3(x, 3.75f, 0), swatch);
            Model(new BoxMesh { Size = new Vector3(2.23f, 0.4f, 1f) }, new Vector3(x, 5.1f, -1), swatch);
        }
        var overlayLayer = new CanvasLayer();
        AddChild(overlayLayer);
        Apply(3, 0);
        overlayLayer.AddChild(_overlay);
    }

    private void Model(Mesh mesh, Vector3 position, Color color)
    {
        AddChild(new MeshInstance3D
        {
            Mesh = mesh, Position = position,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.8f }
        });
    }

    private void Apply(int mode, int family) =>
        _overlay.Apply(mode, 9, 1.06f, 1, 1, 1, 1, 1, 0.75f, 0, family);

    private async Task MeasureModes()
    {
        var viewport = GetViewport();
        var viewportRid = viewport.GetViewportRid();
        string directory = ProjectSettings.GlobalizePath("res://captures");
        RenderingServer.ViewportSetMeasureRenderTime(viewportRid, true);
        var csv = new List<string> { "mode,frame,gpu_ms,wall_ms" };
        string[] modes = { "raw", "mono", "colour", "cinematic", "diagnostic", "runes" };
        for (int i = 0; i < modes.Length; i++)
        {
            string label = modes[i];
            Apply(i == 5 ? 3 : i, i == 5 ? 1 : 0);
            for (int n = 0; n < WarmupFrames; n++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            var gpu = new List<double>(SampleFrames);
            var wall = new List<double>(SampleFrames);
            ulong last = Time.GetTicksUsec();
            for (int n = 0; n < SampleFrames; n++)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                ulong now = Time.GetTicksUsec();
                gpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewportRid));
                wall.Add((now - last) / 1000.0);
                last = now;
                csv.Add(FormattableString.Invariant($"{label},{n},{gpu[^1]:F4},{wall[^1]:F4}"));
            }
            // Only the test harness reads pixels. Capture follows the measurement window.
            using var image = viewport.GetTexture().GetImage();
            if (image.SavePng(Path.Combine(directory, label + ".png")) != Error.Ok)
                throw new IOException($"No se pudo escribir {label}.png");
            gpu.Sort();
            wall.Sort();
            GD.Print(FormattableString.Invariant($"POC {label} {image.GetWidth()}x{image.GetHeight()} | GPU ms p50 {Percentile(gpu, 0.50):F3} p95 {Percentile(gpu, 0.95):F3} | wall ms p50 {Percentile(wall, 0.50):F3} p95 {Percentile(wall, 0.95):F3}"));
        }
        File.WriteAllLines(Path.Combine(directory, "samples.csv"), csv);
    }

    private static double Percentile(List<double> sortedSamples, double quantile)
    {
        double index = (sortedSamples.Count - 1) * quantile;
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);
        return sortedSamples[lower] + (sortedSamples[upper] - sortedSamples[lower]) * (index - lower);
    }
}
