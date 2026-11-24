using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

internal static class FrameBenchmark
{
    internal static async Task<string> Sample(Node owner, string name, double seconds)
    {
        SceneTree tree = owner.GetTree();
        Rid viewport = owner.GetViewport().GetViewportRid();
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        Engine.MaxFps = 0;
        RenderingServer.ViewportSetMeasureRenderTime(viewport, true);
        for (int i = 0; i < 160; i++) await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var samples = new List<double>();
        var gpu = new List<double>();
        var timer = Stopwatch.StartNew();
        double previous = timer.Elapsed.TotalMilliseconds;
        while (timer.Elapsed.TotalSeconds < seconds || samples.Count < 120)
        {
            await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            double now = timer.Elapsed.TotalMilliseconds;
            samples.Add(now - previous);
            previous = now;
            gpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport));
        }

        samples.Sort();
        gpu.Sort();
        return FormattableString.Invariant($"{name} size={DisplayServer.WindowGetSize()} adapter={RenderingServer.GetVideoAdapterName()} frames={samples.Count} seconds={timer.Elapsed.TotalSeconds:F3} fps={1000/samples.Average():F2} median_ms={samples[samples.Count/2]:F2} p95_ms={samples[(int)(samples.Count*.95)]:F2} p99_ms={samples[(int)(samples.Count*.99)]:F2} gpu_median_ms={gpu[gpu.Count/2]:F2} gpu_p95_ms={gpu[(int)(gpu.Count*.95)]:F2}");
    }
}
