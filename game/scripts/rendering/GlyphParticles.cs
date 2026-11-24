using Godot;

public static class GlyphParticles
{
    public static QuadMesh Mesh(string glyph, Color color, float size)
    {
        var material = new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>($"res://assets/fonts/particle_{glyph}.png"),
            AlbedoColor = color,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            VertexColorUseAsAlbedo = true
        };
        return new QuadMesh { Size = new(size, size), Material = material };
    }
}
