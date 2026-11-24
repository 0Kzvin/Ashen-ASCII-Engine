using Godot;
using System.Collections.Generic;

internal sealed class ArtisticSurfaces
{
    readonly List<(MeshInstance3D Mesh, StandardMaterial3D Source, ShaderMaterial Ink)> pieces = new();
    bool shown;

    internal ArtisticSurfaces(Node3D rig, float width, Material cloth, Material seam, Material fire, Material core)
    {
        Shader shader = GD.Load<Shader>("res://shaders/artistic_surface.gdshader");
        Texture2D atlas = GD.Load<Texture2D>("res://assets/fonts/artistic_atlas.png");
        var materials = new Dictionary<StandardMaterial3D, ShaderMaterial>();
        foreach (var node in rig.FindChildren("*", "MeshInstance3D", true, false))
        {
            var mesh = (MeshInstance3D)node;
            if (mesh.MaterialOverride is not StandardMaterial3D source) continue;
            if (!materials.TryGetValue(source, out var ink))
            {
                int family = source == cloth || source == seam ? 1 : source == fire || source == core ? 2 : 0;
                ink = new ShaderMaterial { Shader = shader };
                ink.SetShaderParameter("glyph_atlas", atlas);
                ink.SetShaderParameter("paint", source.AlbedoColor);
                ink.SetShaderParameter("family", family);
                ink.SetShaderParameter("glyph_width", width);
                ink.SetShaderParameter("surface_tag", .185f + family * .024f);
                ink.SetShaderParameter("emission", family == 2 ? .66f : .0f);
                materials.Add(source, ink);
            }
            pieces.Add((mesh, source, ink));
        }
    }

    internal void Show(bool show)
    {
        if (shown == show) return;
        shown = show;
        foreach (var (mesh, source, ink) in pieces)
            mesh.MaterialOverride = show ? ink : source;
    }
}
