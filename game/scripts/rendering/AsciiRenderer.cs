using Godot;

/// <summary>Fullscreen GPU composition. Place on a CanvasLayer between the world and the HUD.</summary>
public partial class AsciiRenderer : ColorRect
{
    private ShaderMaterial _composite;
    private ShaderMaterial _v2, _v3;
    private AsciiGeometry _geometry;
    private int _mode = 3;
    public const int AdvancedMode = 5;
    public const int ArtisticMode = 6;
    // Studio indices map to V3 atlas rows plus one. Zero selects by material.
    private static readonly int[] ArtisticFamilies = { 0, 6, 4, 5, 7, 1, 3 };

    public void AttachGeometry(Camera3D camera)
    {
        if (RenderingServer.GetCurrentRenderingMethod() != "forward_plus" || RenderingServer.GetRenderingDevice() == null)
            return;
        _geometry = new AsciiGeometry { Enabled = false };
        _geometry.Prepare();
        camera.Compositor = new Compositor { CompositorEffects = new() { _geometry } };
    }

    public override void _ExitTree() => _geometry?.Release();

    private ShaderMaterial LoadV2()
    {
        if (_v2 != null) return _v2;
        _v2 = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/ascii_v2.gdshader") };
        _v2.SetShaderParameter("glyph_atlas", GD.Load<Texture2D>("res://assets/fonts/glyph_atlas.png"));
        _v2.SetShaderParameter("coverage_map", GD.Load<Texture2D>("res://assets/fonts/glyph_coverage.png"));
        if (_geometry != null)
        {
            _v2.SetShaderParameter("cell_field", _geometry.Field);
            _v2.SetShaderParameter("has_geometry", true);
        }
        return _v2;
    }

    private ShaderMaterial LoadV3()
    {
        if (_v3 != null) return _v3;
        _v3 = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/ascii_v3.gdshader") };
        _v3.SetShaderParameter("glyph_atlas", GD.Load<Texture2D>("res://assets/fonts/artistic_atlas.png"));
        if (_geometry != null)
        {
            _v3.SetShaderParameter("cell_field", _geometry.Field);
            _v3.SetShaderParameter("surface_mask", _geometry.Mask);
            _v3.SetShaderParameter("has_geometry", true);
        }
        return _v3;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        LoadComposite();
        Material ??= _composite;
        Visible = _mode != 0;
    }

    private void LoadComposite()
    {
        if (_composite != null) return;
        _composite = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/ascii_composite.gdshader") };
        _composite.SetShaderParameter("glyph_atlas", GD.Load<Texture2D>("res://assets/fonts/glyph_atlas.png"));
    }

    public void Apply(int mode, float cell, float density, float contrast, float brightness,
        float saturation, float exposure, float edges, float stability, int palette, int glyphSet, bool hybrid = true)
    {
        _mode = Mathf.Clamp(mode, 0, ArtisticMode);
        Visible = _mode != 0;
        LoadComposite();
        ShaderMaterial material = _mode switch {
            AdvancedMode => LoadV2(),
            ArtisticMode => LoadV3(),
            _ => _composite
        };
        if (_mode == ArtisticMode) material.SetShaderParameter("preserve_material_ink", hybrid);
        Material = material;
        if (_geometry != null)
        {
            _geometry.Cell = Mathf.Clamp(cell, 4f, 16f);
            _geometry.Aspect = _mode == ArtisticMode ? 1.30f : 1.36f;
            _geometry.Artistic = _mode == ArtisticMode;
            _geometry.Enabled = _mode >= AdvancedMode;
        }
        if (material == _composite) material.SetShaderParameter("display_mode", _mode);
        material.SetShaderParameter("cell_width", Mathf.Clamp(cell, 4f, 16f));
        material.SetShaderParameter("density", Mathf.Clamp(density, 0f, 2f));
        material.SetShaderParameter("contrast", Mathf.Clamp(contrast, 0f, 2f));
        material.SetShaderParameter("brightness", Mathf.Clamp(brightness, 0f, 2f));
        material.SetShaderParameter("saturation", Mathf.Clamp(saturation, 0f, 2f));
        material.SetShaderParameter("exposure", Mathf.Clamp(exposure, 0f, 2f));
        material.SetShaderParameter("edge_strength", Mathf.Clamp(edges, 0f, 2f));
        material.SetShaderParameter("stability", Mathf.Clamp(stability, 0f, 1f));
        material.SetShaderParameter("palette_index", Mathf.Clamp(palette, 0, 2));
        int family = Mathf.Clamp(glyphSet, 0, 6);
        material.SetShaderParameter("glyph_family", _mode == ArtisticMode ? ArtisticFamilies[family] : family);
    }
}
