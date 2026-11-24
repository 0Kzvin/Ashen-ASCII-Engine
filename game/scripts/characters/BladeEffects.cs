using Godot;
using System.Collections.Generic;

internal sealed class BladeEffects
{
    private readonly record struct Slice(Vector3 Inner, Vector3 Tip, float Time);
    private readonly List<Slice> _history = new(20);
    private readonly CpuParticles3D _embers, _impact;
    private readonly ImmediateMesh _ribbon = new();
    private readonly StandardMaterial3D _ribbonMaterial;
    private readonly ShaderMaterial _artisticRibbon;
    private readonly Color _color;
    private readonly Node3D _heel, _tip;
    private bool _enabled = true;
    internal bool Artistic { get; set; }

    internal BladeEffects(Knight parent, KnightRig rig, bool enemy)
    {
        _heel=rig.BladeHeel; _tip=rig.BladeTip;
        _color=enemy?new("ff512b"):new("ffb335");
        var mesh=GlyphParticles.Mesh("spark", Colors.White, .125f);
        var gradient=new Gradient
        {
            Offsets=new float[]{0,.35f,1},
            Colors=new Color[]{new("fff7bd"),_color,new(_color,0)}
        };
        _embers=new CpuParticles3D
        {
            Name="BladeCinders", Emitting=true, Amount=26, Lifetime=.40f, LocalCoords=false,
            Mesh=mesh, ColorRamp=gradient, EmissionShape=CpuParticles3D.EmissionShapeEnum.Box,
            EmissionBoxExtents=new(.036f,.50f,.015f), Direction=Vector3.Up, Spread=34,
            InitialVelocityMin=.36f, InitialVelocityMax=1.3f, Gravity=new(0,1.45f,0),
            ScaleAmountMin=.52f,ScaleAmountMax=1.28f,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off
        };
        _heel.AddChild(_embers); _embers.Position=new(0,.48f,0);
        _impact=new CpuParticles3D
        {
            Name="HitSparks",Emitting=false,OneShot=true,Amount=27,Lifetime=.38f,Explosiveness=1,
            LocalCoords=false,Mesh=mesh,ColorRamp=gradient,Direction=Vector3.Up,Spread=72,
            InitialVelocityMin=2.0f,InitialVelocityMax=5.2f,Gravity=new(0,-8,0),
            ScaleAmountMin=.86f,ScaleAmountMax=2.0f,TopLevel=true,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off
        };
        parent.AddChild(_impact);
        _ribbonMaterial=new StandardMaterial3D
        {
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled,
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,
            VertexColorUseAsAlbedo=true
        };
        var trail=new MeshInstance3D {Name="SwordTrail",Mesh=_ribbon,TopLevel=true,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off };
        parent.AddChild(trail);
        trail.GlobalTransform=Transform3D.Identity;
        _artisticRibbon = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/artistic_trail.gdshader") };
        _artisticRibbon.SetShaderParameter("glyph_atlas", GD.Load<Texture2D>("res://assets/fonts/artistic_atlas.png"));
    }

    internal void SetIntensity(float value)
    {
        bool enabled=value>.01f;
        int count=Mathf.Max(1,Mathf.RoundToInt(26*Mathf.Clamp(value,0,2)));
        if(_embers.Amount!=count) _embers.Amount=count;
        _embers.Emitting=enabled; _enabled=enabled;
    }

    internal void Hit(Vector3 point)
    {
        if(!_enabled) return;
        _impact.GlobalPosition=point; _impact.Restart(); _impact.Emitting=true;
    }

    internal void Tick(float time,bool active)
    {
        while(_history.Count>0 && time-_history[0].Time>.14f)
            _history.RemoveAt(0);
        if(active && _enabled)
            _history.Add(new(_heel.GlobalPosition.Lerp(_tip.GlobalPosition,.26f),_tip.GlobalPosition,time));
        _ribbon.ClearSurfaces();
        if(_history.Count<2) return;
        _ribbon.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip, Artistic ? _artisticRibbon : _ribbonMaterial);
        foreach(var slice in _history)
        {
            float alpha=Mathf.Clamp(1-(time-slice.Time)/.14f,0,1);
            _ribbon.SurfaceSetUV(new Vector2(1-alpha,0));
            _ribbon.SurfaceSetColor(new Color(_color,(Artistic ? .09f : .028f)*alpha));
            _ribbon.SurfaceAddVertex(slice.Inner);
            _ribbon.SurfaceSetUV(new Vector2(1-alpha,1));
            _ribbon.SurfaceSetColor(new Color(_color,(Artistic ? .54f : .40f)*alpha));
            _ribbon.SurfaceAddVertex(slice.Tip);
        }
        _ribbon.SurfaceEnd();
        // Keep a thin bright rim and let the larger ribbon fade into the background.
        _ribbon.SurfaceBegin(Mesh.PrimitiveType.LineStrip,_ribbonMaterial);
        foreach(var slice in _history)
        {
            float alpha=Mathf.Clamp(1-(time-slice.Time)/.14f,0,1);
            _ribbon.SurfaceSetColor(new Color(new Color("#ffe2a0"),.82f*alpha));
            _ribbon.SurfaceAddVertex(slice.Tip);
        }
        _ribbon.SurfaceEnd();
    }
}
