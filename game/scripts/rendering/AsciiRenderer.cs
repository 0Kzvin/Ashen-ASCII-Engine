using Godot;

/// <summary>Fullscreen GPU composition. Place on a CanvasLayer between the world and the HUD.</summary>
public partial class AsciiRenderer : ColorRect
{
    private ShaderMaterial _composite;
    private int _mode = 3;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        LoadComposite();
        Material = _composite;
        Visible = _mode != 0;
    }

    private void LoadComposite()
    {
        if (_composite != null) return;
        _composite = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/ascii_composite.gdshader") };
        _composite.SetShaderParameter("glyph_atlas", GD.Load<Texture2D>("res://assets/fonts/glyph_atlas.png"));
    }

    public void Apply(int mode, float cell, float density, float contrast, float brightness,
        float saturation, float exposure, float edges, float stability, int palette, int glyphSet)
    {
        _mode = Mathf.Clamp(mode, 0, 4);
        Visible = _mode != 0;
        LoadComposite();
        _composite.SetShaderParameter("display_mode", _mode);
        _composite.SetShaderParameter("cell_width", Mathf.Clamp(cell, 4f, 16f));
        _composite.SetShaderParameter("density", Mathf.Clamp(density, 0f, 2f));
        _composite.SetShaderParameter("contrast", Mathf.Clamp(contrast, 0f, 2f));
        _composite.SetShaderParameter("brightness", Mathf.Clamp(brightness, 0f, 2f));
        _composite.SetShaderParameter("saturation", Mathf.Clamp(saturation, 0f, 2f));
        _composite.SetShaderParameter("exposure", Mathf.Clamp(exposure, 0f, 2f));
        _composite.SetShaderParameter("edge_strength", Mathf.Clamp(edges, 0f, 2f));
        _composite.SetShaderParameter("stability", Mathf.Clamp(stability, 0f, 1f));
        _composite.SetShaderParameter("palette_index", Mathf.Clamp(palette, 0, 2));
        _composite.SetShaderParameter("glyph_family", Mathf.Clamp(glyphSet, 0, 6));
    }
}
