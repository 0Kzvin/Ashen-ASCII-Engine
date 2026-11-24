using Godot;
using System;
using System.Collections.Generic;

/// <summary>The drowned cloister. Local floor level is zero and north is negative Z.</summary>
public partial class Ruins : Node3D
{
    [Export] public PackedScene DecorationScene { get; set; }
    private readonly RandomNumberGenerator _rng = new() { Seed = 39132 };
    private readonly Dictionary<(Mesh, Material), List<Transform3D>> _batches = new();
    private readonly List<(OmniLight3D Light, float Energy, float Phase)> _lamps = new();
    private readonly List<(CpuParticles3D Emitter, int Count)> _emitters = new();
    private readonly BoxMesh _cube = new() { Size = Vector3.One };
    private readonly CylinderMesh _shaft = new() { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1, RadialSegments = 8, Rings = 0 };
    private readonly CylinderMesh _spire = new() { TopRadius = 0, BottomRadius = 0.5f, Height = 1, RadialSegments = 8, Rings = 0 };
    private readonly SphereMesh _orb = new() { Radius = 0.5f, Height = 1, RadialSegments = 12, Rings = 6 };
    private readonly StandardMaterial3D[] _stone = new StandardMaterial3D[5];
    private readonly StandardMaterial3D[] _paving = new StandardMaterial3D[4];
    private StandardMaterial3D _mortar = null!, _carving = null!, _iron = null!, _moss = null!, _leaf = null!, _bark = null!, _gold = null!;
    private StandardMaterial3D _rune = null!, _flame = null!, _flameCore = null!, _cloth = null!, _distant = null!;
    private Godot.Environment _sky = null!;
    private SpotLight3D _courtFill;
    private DirectionalLight3D _moonlight = null!;
    private float _lightLevel = 1, _fogLevel = 1, _particleLevel = 1, _glowLevel = 1;
    private double _time;

    public override void _Ready()
    {
        string[] shades = { "455360", "536370", "657786", "758491", "394956" };
        for (int i = 0; i < shades.Length; i++) _stone[i] = Surface(shades[i]);
        string[] pavers = { "40515d", "46565f", "485966", "394a57" };
        var grain = StoneGrain();
        for (int i = 0; i < pavers.Length; i++) _paving[i] = Surface(pavers[i]);
        foreach (var set in new[]{_stone,_paving})
        foreach (var mat in set)
        {
            mat.AlbedoTexture = grain;
            mat.Uv1Triplanar = true;
            mat.Uv1WorldTriplanar = true;
            mat.Uv1Scale = new(0.43f,0.43f,0.43f);
        }
        _carving = Surface("8b9da5");
        _mortar = Surface("263843");
        _distant = Surface("283c4a");
        _iron = Surface("474749", 0.62f, 0.42f);
        _gold = Surface("be9454", 0.56f, 0.38f);
        _moss = Surface("435a38");
        _leaf = Surface("77905a");
        _bark = Surface("4c4642");
        _cloth = Surface("98374a");
        _rune = Glow("63dee0", 1.9f);
        _flame = Glow("ff872e", 3.2f);
        _flameCore = Glow("ffdfa0", 4.2f);
        Sky();
        Ground();
        Sanctuary();
        Cloister();
        BuildDecoration();
        Torch(-3.1f, -2.95f, 1.55f);
        Torch(3.12f, -3.1f, 1.5f);
        Torch(-6.9f, 2.55f, 0.94f);
        FlushBatches();
        SetAtmosphere(_lightLevel, _fogLevel, _particleLevel, _glowLevel);
    }

    public void SetAtmosphere(float light, float fog, float particles, float glow)
    {
        _lightLevel = Mathf.Clamp(light, 0, 2);
        _fogLevel = Mathf.Clamp(fog, 0, 2);
        _particleLevel = Mathf.Clamp(particles, 0, 2);
        _glowLevel = Mathf.Clamp(glow, 0, 2);
        if (_sky == null) return;
        _moonlight.LightEnergy = 1.16f * _lightLevel;
        _courtFill.LightEnergy = 1.55f * _lightLevel;
        _sky.AmbientLightEnergy = 0.15f + 0.40f * _lightLevel;
        _sky.FogEnabled = _fogLevel > 0.005f;
        _sky.FogDensity = 0.009f * _fogLevel;
        _sky.GlowEnabled = _glowLevel > 0.005f;
        _sky.GlowIntensity = 0.55f * _glowLevel;
        foreach (var (emitter, count) in _emitters)
        {
            int amount = Math.Max(1, Mathf.RoundToInt(count * _particleLevel));
            if (emitter.Amount != amount) emitter.Amount = amount;
            emitter.Emitting = _particleLevel > 0.01f;
        }
        UpdateLamps();
    }

    public override void _Process(double delta)
    {
        _time += delta;
        UpdateLamps();
    }

    private void UpdateLamps()
    {
        foreach (var (lamp, energy, phase) in _lamps)
        {
            float t = (float)_time + phase;
            lamp.LightEnergy = energy * _lightLevel * (1f + 0.045f * Mathf.Sin(t * 8) + 0.025f * Mathf.Sin(t * 13.8f));
        }
    }

    private static StandardMaterial3D Surface(string hex, float metal = 0, float rough = 0.89f) =>
        new() { AlbedoColor = new Color(hex), Metallic = metal, Roughness = rough };

    private static StandardMaterial3D Glow(string hex, float energy) =>
        new() { AlbedoColor = new Color(hex), EmissionEnabled = true, Emission = new Color(hex), EmissionEnergyMultiplier = energy };

    private static Texture2D StoneGrain()
    {
        // World-mapped chisel grain survives rotations and keeps adjacent blocks distinct.
        var noise=new FastNoiseLite {Seed=742,Frequency=0.044f};
        var image=Image.CreateEmpty(128,128,false,Image.Format.Rgb8);
        for(int y=0;y<128;y++) for(int x=0;x<128;x++)
        {
            float low=noise.GetNoise2D(x,y);
            float seam=Mathf.Abs(noise.GetNoise2D(x*0.7f+y*0.38f,y*2.5f));
            float grain=0.77f+0.13f*low+0.14f*Mathf.SmoothStep(0.01f,0.12f,seam)+0.06f*noise.GetNoise2D(x*4,y*4);
            image.SetPixel(x,y,new(grain,grain,grain));
        }
        image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }

    private float Range(float a, float b) => _rng.RandfRange(a, b);
    private StandardMaterial3D Ashlar() => _stone[_rng.RandiRange(0, _stone.Length - 1)];

    private void Place(Mesh mesh, Material material, Vector3 position, Vector3 size, Vector3 euler = default)
    {
        var key = (mesh, material);
        if (!_batches.TryGetValue(key, out var list)) _batches[key] = list = new();
        list.Add(new Transform3D(Basis.FromEuler(euler) * Basis.FromScale(size), position));
    }

    private void Box(float x, float y, float z, float w, float h, float d, Material mat, float yaw = 0) =>
        Place(_cube, mat, new(x,y,z), new(w,h,d), new(0,yaw,0));

    private void Post(float x, float y, float z, float width, float height, Material mat) =>
        Place(_shaft, mat, new(x,y,z), new(width,height,width));

    private void Line(Vector3 a, Vector3 b, float width, Material mat, Mesh mesh = null)
    {
        Vector3 delta = b - a;
        if (delta.LengthSquared() < 0.00001f) return;
        Basis rotation = new(new Quaternion(Vector3.Up, delta.Normalized()));
        var key = (mesh ?? _shaft, mat);
        if (!_batches.TryGetValue(key, out var list)) _batches[key] = list = new();
        list.Add(new Transform3D(rotation * Basis.FromScale(new(width,delta.Length(),width)), (a+b)*0.5f));
    }

    private void FlushBatches()
    {
        foreach (var (key, transforms) in _batches)
        {
            var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = key.Item1, InstanceCount = transforms.Count };
            for (int i = 0; i < transforms.Count; i++) mm.SetInstanceTransform(i, transforms[i]);
            AddChild(new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = key.Item2 });
        }
        _batches.Clear();
    }

    private void Solid(string name, Vector3 point, Vector3 size)
    {
        var body = new StaticBody3D { Name = name, Position = point };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        AddChild(body);
    }

    private void Sky()
    {
        _sky = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("0a1225"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("aab9d4"),
            FogLightColor = new Color("252f47"), FogSkyAffect = 0.34f, FogLightEnergy = 0.58f,
            GlowBloom = 0.065f, GlowHdrThreshold = 1.32f
        };
        AddChild(new WorldEnvironment { Environment = _sky });
        _moonlight = new DirectionalLight3D
        {
            LightColor = new Color("c5dbff"), RotationDegrees = new(-46,-28,0),
            ShadowEnabled = true, DirectionalShadowMaxDistance = 48, ShadowBias = 0.04f
        };
        AddChild(_moonlight);
        // A soft near-side pool separates the playable figures from the flagstones.
        var fill = new SpotLight3D { Position = new(1.4f, 5.2f, 3.6f),
            LightColor = new Color("#b6c3dc"), LightEnergy = 1.55f, SpotRange = 10.5f,
            SpotAngle = 40, SpotAngleAttenuation = 1.2f };
        AddChild(fill);
        fill.LookAt(new Vector3(0, .8f, .25f));
        _courtFill = fill;
        var lunarSurface=new ShaderMaterial { Shader=new Shader {Code=@"
shader_type spatial;
render_mode unshaded;
varying vec3 coord;
void vertex(){coord=VERTEX;}
void fragment(){
 vec3 q=normalize(coord);
 float sea=0.; float rim=0.;
 for(int i=0;i<18;i++){
  float k=float(i); vec3 axis=normalize(vec3(sin(k*76.31+.5),cos(k*42.13),sin(k*19.23+.7)));
  float d=distance(q,axis); float radius=.13+.18*fract(sin(k*93.7)*4375.45);
  sea+=1.-smoothstep(radius*.64,radius,d);
  rim+=1.-smoothstep(.0,.025,abs(d-radius));
 }
 ALBEDO=mix(vec3(.92,1.08,1.17),vec3(.59,.74,.87),min(1.,sea)*.62)+rim*vec3(.05);
}"}};
        Place(_orb, lunarSurface, new(-25,4.4f,-8.9f), new(3.3f,3.3f,3.3f));
        var stars=Glow("aec5df",1.48f);
        for(int i=0;i<180;i++)
        {
            float alpha=Range(-2.1f,0.58f), r=Range(54,68), size=Range(0.042f,0.13f);
            Place(_orb,stars,new(Mathf.Sin(alpha)*r,Range(3,25),-Mathf.Cos(alpha)*r),new(size,size,size));
        }
        // Narrow lancet openings and stepped gables identify the ruined abbey on the ridge.
        for(int i=0;i<10;i++)
        {
            float x=-16+i*2.22f, z=-16.1f;
            float spring=2.55f+(i>3 && i<7 ? 1.4f : 0);
            Box(x,spring*0.5f,z,0.63f,spring,1.02f,_distant);
            if(i<9 && i!=3 && i!=6)
            {
                for(int side=-1;side<=1;side+=2)
                for(int j=0;j<5;j++)
                {
                    Vector2 a=ArchPoint(1.10f,spring,side,j/5f);
                    Vector2 b=ArchPoint(1.10f,spring,side,(j+1)/5f);
                    Line(new(x+1.11f+a.X,a.Y,z),new(x+1.11f+b.X,b.Y,z),0.58f,_distant,_cube);
                }
            }
            if(i==2 || i==4 || i==7)
            {
                Box(x,spring+0.92f,z,0.74f,2.30f,1.0f,_distant);
                Place(_spire,_distant,new(x,spring+2.71f,z),new(0.87f,1.5f,0.87f));
            }
        }
    }

    private void Ground()
    {
        Terrain();
        MountainRidge(43,0,Surface("1c2c40"));
        MountainRidge(35,1,Surface("182938"));
        Solid("CourtyardFloor",new(0,-0.18f,0),new(36,0.36f,32));
        Solid("WesternBoundary",new(-9.3f,2,0),new(0.5f,4,19));
        Solid("EasternBoundary",new(9.3f,2,0),new(0.5f,4,19));
        Solid("NorthernBoundary",new(0,2,-8.55f),new(19,4,0.5f));
        Solid("SouthernBoundary",new(0,2,7.65f),new(19,4,0.5f));
        Box(0,-0.129f,-0.3f,19.55f,0.2f,16.65f,_mortar);
        for (int row = -8; row <= 7; row++)
        for (int col = -6; col <= 6; col++)
        {
            float x = 1.45f*col + ((row & 1) == 0 ? 0.32f : -0.32f), z = row*0.99f;
            if (x > 4.2f && x < 8.35f && z >= -0.1f && z <= 3.1f) continue;
            float h = Range(0.09f,0.12f);
            Box(x,-0.065f,z,Range(1.38f,1.425f),h,Range(0.92f,0.955f),_paving[_rng.RandiRange(0,3)],Range(-0.009f,0.009f));
            if (_rng.Randf() < 0.12f)
                Box(x+Range(-0.4f,0.4f),0.001f,z+0.477f,Range(0.18f,0.77f),0.018f,Range(0.025f,0.055f),_moss);
        }
        for(int i=0;i<78;i++)
        {
            float x=Range(-19,16),z=Range(-15,13);
            if(Mathf.Abs(x)<9.4f && z>-8.3f && z<7.4f) continue;
            float w=Range(0.28f,1.5f);
            Place(_orb,_paving[i%4],new(x,-0.19f,z),new(w,w*0.38f,w*0.71f),new(0,Range(0,6),0.18f));
            if(i%3==0) Fern(new(x,-0.2f,z),Range(0.3f,0.55f));
        }
        for(int i=0;i<19;i++)
        {
            Box(-9.2f+i*1.02f,-0.059f,7.58f,0.98f,0.12f,0.24f,_paving[i%4]);
            if(i<16)
            {
                Box(-9.63f,-0.059f,-7.9f+i,0.25f,0.14f,0.96f,_paving[i%4]);
                Box(9.63f,-0.059f,-7.9f+i,0.25f,0.14f,0.96f,_paving[i%4]);
            }
        }
        // A bronze compass is set flush into the processional floor.
        Vector3 center = new(0,0.018f,-0.45f);
        for (int i = 0; i < 48; i++)
        {
            float a = Mathf.Tau*i/48, b = Mathf.Tau*(i+1)/48;
            Vector3 ray = new(Mathf.Cos(a),0,Mathf.Sin(a));
            Vector3 next = new(Mathf.Cos(b),0,Mathf.Sin(b));
            Line(center+ray*2.67f,center+next*2.67f,0.035f,_gold);
            if (i % 3 == 0) Line(center+ray*2.44f,center+ray*2.61f,0.046f,_gold);
            if (i % 6 == 0) Line(center+ray*2.52f,center+next*2.52f,0.027f,_rune);
        }
        var crest = new SurfaceTool();
        crest.Begin(Mesh.PrimitiveType.Triangles);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.Tau/8;
            Vector3 dir=new(Mathf.Cos(a),0,Mathf.Sin(a));
            Vector3 tangent=dir.Cross(Vector3.Up);
            Vector3 tip=center+dir*(i%2==0?1.55f:0.97f),root=center+dir*0.28f;
            foreach(int side in new[]{-1,1})
            {
                foreach(var point in new[]{tip,root+tangent*side*0.12f,center})
                {
                    crest.SetNormal(Vector3.Up);
                    crest.SetColor(side==1?new Color("817758"):new Color("4c575a"));
                    crest.AddVertex(point);
                }
            }
        }
        crest.SetMaterial(new StandardMaterial3D {VertexColorUseAsAlbedo=true,CullMode=BaseMaterial3D.CullModeEnum.Disabled,Roughness=0.65f});
        AddChild(new MeshInstance3D{Mesh=crest.Commit()});
        Water();
    }

    private void Terrain()
    {
        var mat=new ShaderMaterial{Shader=new Shader{Code=@"
shader_type spatial;
render_mode cull_disabled,depth_prepass_alpha;
varying vec3 world;
void vertex(){world=(MODEL_MATRIX*vec4(VERTEX,1.)).xyz;}
float hash(vec2 p){return fract(sin(dot(p,vec2(41.7,127.3)))*4375.61);}
float noise(vec2 p){
 vec2 i=floor(p),u=fract(p);u=u*u*(3.-2.*u);
 return mix(mix(hash(i),hash(i+vec2(1,0)),u.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),u.x),u.y);
}
void fragment(){
 vec2 p=world.xz;
 float field=.62*noise(p*.68)+.25*noise(p*2.3)+.13*noise(p*8.);
 ALBEDO=mix(vec3(.008,.013,.019),vec3(.028,.038,.038),smoothstep(.25,.82,field));
 ROUGHNESS=.97; ALPHA=1.-smoothstep(32.,55.,length(p));
}"}};
        const int sides=76;
        float[] radii={0.01f,8f,13f,18f,25f,36f,56f};
        var geometry=new SurfaceTool();
        geometry.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 Vertex(int ring,int step)
        {
            float a=Mathf.Tau*(step%sides)/sides;
            float radius=radii[ring],waves=Mathf.Sin(3*a+ring*.31f)*.85f+Mathf.Sin(7*a+1.1f)*.45f+Mathf.Sin(11*a)*.18f;
            float d=radius+(ring>2?waves:0);
            float y=ring switch {0 or 1 or 2=>-0.21f,3=>-.75f+waves*.47f,4=>-2.0f+waves*.65f,5=>-3.7f+waves, _=>-8.5f+waves*1.4f};
            return new(Mathf.Cos(a)*d,y,Mathf.Sin(a)*d);
        }
        for(int r=0;r<radii.Length-1;r++) for(int j=0;j<sides;j++)
            Quad(geometry,Vertex(r,j),Vertex(r,j+1),Vertex(r+1,j+1),Vertex(r+1,j),Vector3.Up,Colors.White);
        geometry.SetMaterial(mat);
        AddChild(new MeshInstance3D{Mesh=geometry.Commit(),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
        for(int i=0;i<95;i++)
        {
            bool northSouth=i%3!=0;
            int side=i%2==0?-1:1;
            float x=northSouth?Range(-10.4f,10.4f):side*Range(9.3f,12.4f);
            float z=northSouth?side*Range(7.7f,10.3f):Range(-7.6f,7.8f);
            float scale=Range(.23f,.93f);
            Place(_cube,_paving[i%4],new(x,-0.18f,z),new(scale*1.41f,Range(.15f,.27f),scale*.78f),
                new(Range(-.13f,.13f),Range(-.36f,.36f),Range(-.11f,.11f)));
            if(i%4==0) Fern(new(x+.25f,-.20f,z),Range(.28f,.55f));
        }
    }

    private void MountainRidge(float radius,float phase,Material mat)
    {
        var st=new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 Point(int i,int tier)
        {
            float a=-2.7f+i*0.045f;
            float shape=Mathf.Sin(a*5+phase*1.7f)*1.28f+Mathf.Sin(a*13+phase)*.65f+Mathf.Sin(a*33.62f)*.26f;
            float top=2.8f+shape-phase*1.9f;
            float r=radius+(tier==0?-6f:tier==1?-2.6f:0f)+Mathf.Sin(a*11+phase)*.68f;
            float y=tier==0?-6.3f:tier==1?top*.29f-1.5f:top;
            return new(Mathf.Sin(a)*r,y,-Mathf.Cos(a)*r);
        }
        for(int i=0;i<92;i++) for(int tier=0;tier<2;tier++)
        {
            Vector3 a=Point(i,tier),b=Point(i+1,tier),c=Point(i+1,tier+1),d=Point(i,tier+1);
            Vector3 slope=(a-b).Cross(c-b).Normalized();
            if(slope.Y<0) slope=-slope;
            Quad(st,a,b,c,d,slope,Colors.White);
        }
        ((BaseMaterial3D)mat).CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        st.SetMaterial(mat);
        AddChild(new MeshInstance3D{Mesh=st.Commit(),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
    }

    private void Water()
    {
        var shader = new Shader { Code = @"
shader_type spatial;
render_mode cull_disabled;
varying vec3 world;
void vertex(){world=(MODEL_MATRIX*vec4(VERTEX,1.)).xyz;}
void fragment(){
 vec2 p=world.xz; float t=TIME;
 float ripple=sin(p.y*18.+sin(p.x*5.-t*1.8)*1.6+t*2.1);
 float second=sin(p.x*4.+p.y*9.-t*.8);
 float glint=smoothstep(.86,.98,ripple)*smoothstep(.34,.92,second);
 ALBEDO=mix(vec3(.014,.092,.18),vec3(.13,.43,.55),glint);
 EMISSION=vec3(.012,.054,.115)+glint*vec3(.04,.12,.15);
 ROUGHNESS=.27; METALLIC=.38;
}" };
        Box(6.26f,-0.026f,1.48f,4.45f,0.028f,4.06f,new ShaderMaterial { Shader = shader });
        for (int i=0;i<6;i++)
        {
            float z = -0.57f+i*0.8f;
            Box(4.12f,0.025f,z,0.17f,0.12f,0.77f,_carving);
            Box(8.4f,0.025f,z,0.17f,0.12f,0.77f,_carving);
        }
        for (int i=0;i<5;i++)
        {
            Box(4.52f+i*0.86f,0.025f,-0.62f,0.82f,0.12f,0.18f,_carving);
            Box(4.52f+i*0.86f,0.025f,3.58f,0.82f,0.12f,0.18f,_carving);
        }
    }

    private static Vector2 ArchPoint(float radius, float spring, int side, float fraction)
    {
        float angle = Mathf.Pi/3 * fraction;
        return new(side*radius*(2*Mathf.Cos(angle)-1),spring+2*radius*Mathf.Sin(angle));
    }

    // The joints follow the pointed arch. Each voussoir has front, rear and reveal faces.
    private void Arch(float x, float z, float radius, float spring, float width, float depth, bool pale = false, int blocks = 8)
    {
        var builder = new SurfaceTool();
        builder.Begin(Mesh.PrimitiveType.Triangles);
        foreach (int side in new[] { -1, 1 })
        for (int step=0; step<blocks; step++)
        {
            float a=(step+0.035f)/blocks, b=(step+0.965f)/blocks;
            Vector2 p=ArchPoint(radius,spring,side,a), q=ArchPoint(radius,spring,side,b);
            Vector2 r=ArchPoint(radius+width,spring,side,b), s=ArchPoint(radius+width,spring,side,a);
            Vector3 front(Vector2 k) => new(x+k.X,k.Y,z+depth/2);
            Vector3 back(Vector2 k) => new(x+k.X,k.Y,z-depth/2);
            float tint = Range(0.77f,1);
            Color c = new(tint,tint,tint);
            Quad(builder,front(p),front(q),front(r),front(s),Vector3.Back,c);
            Quad(builder,back(p),back(q),back(r),back(s),Vector3.Forward,c);
            Quad(builder,front(p),back(p),back(q),front(q),new(-side,-0.55f,0),c);
            Quad(builder,front(r),back(r),back(s),front(s),new(side,0.7f,0),c);
            Quad(builder,front(p),front(s),back(s),back(p),Vector3.Down,c);
            Quad(builder,front(q),front(r),back(r),back(q),Vector3.Up,c);
        }
        var mat = (StandardMaterial3D)(pale ? _carving : _stone[2]).Duplicate();
        mat.VertexColorUseAsAlbedo = true;
        mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        builder.SetMaterial(mat);
        AddChild(new MeshInstance3D { Mesh = builder.Commit() });
    }

    private static void Quad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
    {
        normal = normal.Normalized();
        bool flip = (b-a).Cross(c-a).Dot(normal)>0;
        foreach (Vector3 v in flip ? new[]{a,c,b,a,d,c} : new[]{a,b,c,a,c,d})
        { st.SetNormal(normal); st.SetColor(color); st.AddVertex(v); }
    }

    private void Sanctuary()
    {
        const float z = -5.18f;
        Arch(0,z,1.37f,2.04f,0.43f,0.96f,false,10);
        Arch(0,z+0.48f,1.36f,2.04f,0.11f,0.13f,true,12);
        Arch(0,z+0.44f,1.68f,2.04f,0.16f,0.23f,true,12);
        Arch(0,z-0.32f,1.20f,2.04f,0.17f,0.24f,true,10);
        Gable(z-0.42f);
        for (int side=-1;side<=1;side+=2)
        {
            float x=side*1.64f;
            Column(x,z,2.04f,0.80f,true);
            Column(side*1.36f,z+0.48f,2.04f,0.24f,false);
            Tower(side*2.42f,z-0.10f,side == -1 ? 5.70f : 4.78f);
            Banner(side*2.42f,z+0.53f,side == -1 ? 4.75f : 4.15f,0.55f,1.57f);
            Column(side*4.64f,z-0.30f,1.72f,0.65f,true);
            Arch(side*3.65f,z-0.34f,0.92f,1.72f,0.25f,0.55f);
            Arch(side*3.65f,z-0.04f,0.91f,1.72f,0.065f,0.12f,true);
            for (int i=0;i<6;i++)
            {
                Vector2 p=ArchPoint(1.56f,2.04f,side,(i+0.5f)/6f);
                Vector3 point=new(p.X,p.Y,z+0.53f);
                Place(_cube,_rune,point,new(0.052f,0.075f,0.023f),new(0,0,side*(0.1f+i*0.14f)));
            }
        }
        Box(0,0.041f,z+0.25f,3.28f,0.105f,1.26f,_carving);
        Box(0,0.087f,z-0.1f,2.96f,0.13f,1.07f,_stone[2]);
        Portal(z-0.24f);
        Lamp(new(0,1.52f,z+0.65f),new Color("72c8ff"),2.4f,5.2f);
        Lamp(new(0,3.15f,z-0.15f),new Color("a28dff"),1.1f,3.7f);
    }

    private void Gable(float z)
    {
        var back=new SurfaceTool();
        back.Begin(Mesh.PrimitiveType.Triangles);
        foreach(var v in new[]{new Vector3(-1.55f,3.9f,z),new Vector3(0,6.20f,z),new Vector3(1.55f,3.9f,z)})
        {back.SetNormal(Vector3.Back);back.AddVertex(v);}
        back.SetMaterial(_stone[1]);
        AddChild(new MeshInstance3D{Mesh=back.Commit()});
        Vector3 rose=new(0,5.37f,z+0.08f);
        Place(_orb,Glow("355c76",0.68f),rose,new(.84f,.84f,.055f));
        for(int side=-1;side<=1;side+=2)
        {
            Vector3 a=new(side*1.59f,3.91f,z+0.085f),b=new(0,6.23f,z+0.085f);
            Line(a,b,0.135f,_carving,_cube);
            for(int j=1;j<5;j++)
            {
                Vector3 v=a.Lerp(b,j/5f);
                Place(_spire,_carving,v+Vector3.Up*0.08f,new(0.14f,0.28f,0.14f));
            }
        }
        for(int i=0;i<24;i++)
        {
            float a=i*Mathf.Tau/24,b=(i+1)*Mathf.Tau/24;
            Vector3 dir=new(Mathf.Cos(a),Mathf.Sin(a),0), next=new(Mathf.Cos(b),Mathf.Sin(b),0);
            Line(rose+dir*0.43f,rose+next*0.43f,0.079f,_carving);
            if(i%3==0) Line(rose+dir*0.09f+Vector3.Back*.037f,rose+dir*0.39f,0.036f,_carving);
        }
        Place(_orb,_rune,rose+Vector3.Back*0.049f,new(0.135f,0.135f,0.042f));
        Place(_spire,_carving,new(0,6.42f,z+0.085f),new(.21f,.44f,.21f));
    }

    private void Column(float x,float z,float height,float w,bool collide)
    {
        Box(x,0.13f,z,w*1.54f,0.26f,w*1.49f,_stone[1]);
        Box(x,0.30f,z,w*1.31f,0.13f,w*1.27f,_carving);
        Box(x,height*0.5f,z,w*0.73f,height,w*0.72f,_stone[1]);
        foreach (int dx in new[]{-1,1}) foreach (int dz in new[]{-1,1})
            Post(x+dx*w*0.32f,height*0.5f,z+dz*w*0.32f,w*0.33f,height*0.95f,_stone[3]);
        Box(x,height-0.16f,z,w*1.17f,0.15f,w*1.11f,_carving);
        Box(x,height-0.04f,z,w*1.37f,0.12f,w*1.28f,_stone[2]);
        if (collide) Solid("Pillar",new(x,height/2,z),new(w*1.15f,height,w*1.13f));
    }

    private void Tower(float x,float z,float h)
    {
        float width=1.00f;
        Box(x,0.22f,z,1.49f,0.44f,1.5f,_stone[2]);
        for(int i=0;i<11;i++)
        {
            float y=(i+0.5f)*h/11;
            Box(x,y,z,width,h/11-0.016f,1.02f,Ashlar());
            if (i%3 == 0) Box(x,y+0.16f,z,1.13f,0.11f,1.19f,_carving);
        }
        foreach (int side in new[]{-1,1})
        {
            Post(x+side*0.42f,h/2,z+0.4f,0.22f,h,_stone[3]);
            Place(_spire,_carving,new(x+side*0.44f,h+0.45f,z+0.35f),new(0.27f,0.93f,0.27f));
        }
        Box(x,h,z,1.22f,0.23f,1.23f,_carving);
        Post(x,h+0.29f,z,0.74f,0.52f,_stone[1]);
        Place(_spire,_stone[3],new(x,h+1.02f,z),new(0.89f,1.28f,0.89f));
        Line(new(x,h+1.62f,z),new(x,h+2.02f,z),0.045f,_gold);
        Solid("GateTower",new(x,h/2,z),new(1.19f,h,1.24f));
    }

    private void Banner(float x,float z,float y,float w,float h)
    {
        Line(new(x-w*0.70f,y+0.04f,z),new(x+w*0.70f,y+0.04f,z),0.066f,_gold);
        var st=new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 cloth(float u,float v) => new(x+(u-0.5f)*w,y-v*(h-0.29f*Mathf.Abs(2*u-1)),
            z+0.058f*Mathf.Sin(u*Mathf.Pi*3.4f+v*1.25f)+0.036f*v);
        for(int col=0;col<8;col++) for(int row=0;row<5;row++)
        {
            float u=col/8f, v=row/5f;
            Quad(st,cloth(u,v),cloth(u+0.125f,v),cloth(u+0.125f,v+0.2f),cloth(u,v+0.2f),Vector3.Back,Colors.White);
        }
        _cloth.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        st.SetMaterial(_cloth);
        AddChild(new MeshInstance3D{Mesh=st.Commit()});
        Vector3 mark = new(x,y-0.59f,z+0.072f);
        Line(mark+Vector3.Up*0.25f,mark+Vector3.Down*0.28f,0.032f,_gold);
        foreach (int side in new[]{-1,1})
        {
            var mid=mark+Vector3.Right*side*0.13f;
            Line(mark+Vector3.Up*0.15f,mid,0.025f,_gold);
            Line(mid,mark+Vector3.Down*0.15f,0.025f,_gold);
        }
    }

    private void Portal(float z)
    {
        var shader = new Shader { Code = @"
shader_type spatial;
render_mode unshaded,cull_disabled;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
void fragment(){
 vec2 uv=UV; vec2 p=(uv-vec2(.5,.49))*vec2(1.,1.36);
 float a=atan(p.y,p.x+.00001),r=length(p),t=TIME*.27;
 float wave=sin(a*3.+r*18.-t*2.+sin(a*5.+t)*.58);
 float veil=pow(max(0.,wave),7.)*smoothstep(.07,.36,r);
 float orbit=exp(-abs(length(p*vec2(1.04,.86))-.34-.014*sin(a*5.-t))*68.);
 float ticks=pow(max(0.,cos(a*19.+t*2.)),8.)*orbit;
 float n=hash(floor(uv*vec2(52.,80.)));
 float star=smoothstep(.995,1.,n)*(.66+.34*sin(t*5.+n*277.));
 float center=exp(-length(p*vec2(4.,1.))*16.);
 vec3 ink=mix(vec3(.018,.017,.079),vec3(.10,.045,.21),uv.y);
 ALBEDO=ink+veil*mix(vec3(.07,.41,.56),vec3(.24,.09,.58),uv.y)+star*vec3(.40,.62,.82);
 EMISSION=orbit*vec3(.017,.23,.35)+ticks*vec3(.05,.37,.63)+center*vec3(.21,.56,.73);
}" };
        var st=new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        void vertex(float x,float y)
        {
            st.SetUV(new(x/2.72f+0.5f,y/4.39f));
            st.SetNormal(Vector3.Back);
            st.AddVertex(new(x,y,z));
        }
        for (int i=0;i<32;i++)
        {
            float a=-1.36f+2.72f*i/32,b=a+2.72f/32;
            float top(float x) => 2.04f+Mathf.Sqrt(Mathf.Max(0,4*1.36f*1.36f-Mathf.Pow(Mathf.Abs(x)+1.36f,2)));
            vertex(a,0);vertex(a,top(a));vertex(b,top(b));
            vertex(a,0);vertex(b,top(b));vertex(b,0);
        }
        st.SetMaterial(new ShaderMaterial{Shader=shader});
        AddChild(new MeshInstance3D{Name="AstralThreshold",Mesh=st.Commit(),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
        Motes(new(0,0.23f,z+0.20f),_rune,22,3.9f,1.15f,false,true);
    }

    private void Cloister()
    {
        // Broken courses step down toward the open courtyard.
        foreach (int side in new[]{-1,1})
        {
            for (int col=0;col<7;col++)
            {
                float x=side*(4.9f+col*0.92f);
                int rows=(col<3 ? 3 : 5)+_rng.RandiRange(-1,2);
                for (int row=0;row<rows;row++)
                {
                    float z=-5.94f-0.14f*col;
                    Box(x+side*((row%2)*0.15f),row*0.39f+0.18f,z,0.90f,0.375f,0.8f,Ashlar());
                    if(row==rows-1 && col%2==0) Box(x,row*0.39f+0.39f,z,0.94f,0.07f,0.85f,_moss);
                }
            }
            Solid("CloisterWall",new(side*7.1f,0.6f,-6.1f),new(4.5f,1.2f,1));
        }
        Column(-6.45f,-2.6f,3.30f,0.98f,true);
        Column(-6.45f,0.95f,1.28f,0.97f,true);
        Column(7.9f,-4.6f,2.45f,0.83f,true);
        Place(_shaft,_stone[2],new(-6.37f,3.60f,-2.58f),new(0.97f,0.65f,0.92f),new(0,0,0.08f));
        // Broken tracery in the west aisle.
        for(int i=0;i<4;i++)
            Box(-7.28f,0.17f+i*0.36f,-2.63f,1.12f,0.34f,0.69f,Ashlar());
        for(int i=0;i<30;i++)
        {
            float side=i%3==0?1:-1;
            float x=side*Range(4.8f,8.6f),z=Range(-5.1f,4.8f);
            if(x>4 && z>-0.8f && z<3.7f) continue;
            float s=Range(0.15f,0.52f);
            Place(_cube,Ashlar(),new(x,s*0.28f,z),new(s*1.43f,s*0.65f,s),
                new(Range(-0.38f,0.38f),Range(0,Mathf.Tau),Range(-0.26f,0.26f)));
        }
        FallenColumn(new(-4.88f,0.36f,2.83f),new(-5.94f,0.36f,4.16f));
        for (int i=0;i<7;i++)
        {
            float x=-8.25f+Range(-0.25f,0.18f),z=-3.5f+i*0.95f;
            Box(x,0.23f,z,0.78f,0.46f,0.92f,Ashlar());
            if(i<5) Box(x,0.65f,z-0.16f,0.76f,0.37f,0.92f,Ashlar());
        }
    }

    private void FallenColumn(Vector3 a,Vector3 b)
    {
        Vector3 delta=b-a;
        for(int i=0;i<4;i++)
        {
            Line(a+delta*(i/4f),a+delta*((i+0.96f)/4),0.77f,Ashlar());
        }
        Line(a-delta*0.08f,a,0.93f,_carving);
        Line(b,b+delta*0.13f,0.93f,_carving);
        var body=new StaticBody3D {Position=(a+b)*0.5f};
        body.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=0.40f,Height=delta.Length()},
            Transform=new Transform3D(new Basis(new Quaternion(Vector3.Up,delta.Normalized())),Vector3.Zero)});
        AddChild(body);
    }

    private void BuildDecoration()
    {
        var scene=DecorationScene;
        foreach (var path in new[]{"res://assets/world/courtyard_decoration.tscn","res://assets/world/courtyard_decoration.glb"})
        {
            if(scene!=null) break;
            if(ResourceLoader.Exists(path,"PackedScene")) scene=GD.Load<PackedScene>(path);
        }
        if(scene!=null)
        {
            var root=scene.Instantiate();
            if(root is Node3D)
            {
                AddChild(root);
                return;
            }
            root.Free();
            GD.PushWarning("Courtyard decoration requires a Node3D root.");
        }
        Garden();
    }

    private void Garden()
    {
        Tree(new(-8.0f,0,-4.45f),4.05f);
        Tree(new(7.55f,0,-7.45f),3.55f);
        Tree(new(-10.75f,-0.15f,0.45f),4.75f);
        Tree(new(-9.5f,-0.1f,-9),4.0f);
        for(int i=0;i<47;i++)
        {
            float x=i%3==0?Range(5.1f,9.1f):Range(-8.8f,-4.4f);
            float z=Range(-5.9f,5.5f);
            if(x>4.1f && z>-0.6f && z<3.5f) continue;
            Fern(new(x,0,z),Range(0.21f,0.53f));
        }
        for(int i=0;i<24;i++)
        {
            float x=Range(7.92f,8.36f),z=Range(-0.43f,3.26f),h=Range(0.21f,0.68f);
            var bottom=new Vector3(x,-0.02f,z);
            var top=bottom+new Vector3(Range(-0.14f,0.14f),h,Range(-0.10f,0.10f));
            Line(bottom,top,0.022f,_leaf);
            if(i%4==0) Line(top,top+Vector3.Up*0.13f,0.047f,_bark);
        }
        foreach (var start in new[]{new Vector3(-2.92f,4.78f,-4.75f),new Vector3(4.63f,2.04f,-4.94f),new Vector3(-6.52f,3.59f,-2.07f)})
        {
            for(int branch=0;branch<4;branch++)
            {
                var last=start+new Vector3(branch*0.085f,0,0);
                int leaves=_rng.RandiRange(8,18);
                for(int i=0;i<leaves;i++)
                {
                    Vector3 p=last+new Vector3(Range(-0.075f,0.075f),-0.119f,0.005f);
                    Line(last,p,0.026f,_moss);
                    Place(_orb,i%3==0?_leaf:_moss,p+new Vector3(0.035f*(i%2==0?1:-1),0,0),
                        new(0.12f,0.16f,0.054f),new(0,0,(i%2==0?1:-1)*0.7f));
                    last=p;
                }
            }
        }
    }

    private void Fern(Vector3 foot,float h)
    {
        for(int i=0;i<6;i++)
        {
            float angle=i*Mathf.Tau/6+Range(-0.35f,0.35f);
            var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            var tip=foot+direction*h*Range(0.43f,1.03f)+Vector3.Up*h*Range(0.55f,1.13f);
            Line(foot,tip,h*0.19f,i%3==0?_leaf:_moss,_spire);
            var mid=(foot+tip)*0.5f;
            var tangent=direction.Cross(Vector3.Up);
            Line(mid,mid+direction*h*0.37f+tangent*h*0.26f+Vector3.Up*h*0.09f,h*0.16f,_leaf,_spire);
        }
    }

    private void Tree(Vector3 foot,float h)
    {
        Vector3 fork=foot+new Vector3(-h*0.06f,h*0.63f,h*0.04f);
        Line(foot,fork,h*0.146f,_bark);
        for(int i=0;i<8;i++)
        {
            float angle=i*2.4f+Range(-0.31f,0.31f);
            Vector3 ray=new(Mathf.Cos(angle),0,Mathf.Sin(angle));
            Vector3 root=foot.Lerp(fork,Range(0.58f,1f));
            Vector3 mid=root+ray*h*Range(0.13f,0.28f)+Vector3.Up*h*Range(0.14f,0.28f);
            Vector3 end=mid+ray*h*Range(0.19f,0.28f)+Vector3.Up*h*Range(0.13f,0.29f);
            Line(root,mid,h*0.072f,_bark);
            Line(mid,end,h*0.053f,_bark,_spire);
            Line(mid,mid+Vector3.Up*h*0.22f+ray*h*0.09f,h*0.031f,_bark,_spire);
            if(i%3!=0)
            {
                for(int j=0;j<3;j++)
                {
                    var twig=end+new Vector3(Range(-0.24f,0.22f),Range(-0.10f,0.32f),Range(-0.20f,0.20f));
                    Line(end-ray*h*0.10f,twig,h*0.019f,_bark,_spire);
                    Place(_orb,_moss,twig,new(h*0.14f,h*0.080f,h*0.14f),new(0,Range(0,6),Range(-0.4f,0.4f)));
                }
            }
        }
    }

    private void Lamp(Vector3 local,Color color,float energy,float distance)
    {
        var light=new OmniLight3D { Position=local, LightColor=color,LightEnergy=energy,OmniRange=distance,OmniAttenuation=1.2f };
        AddChild(light);
        _lamps.Add((light,energy,Range(0,20)));
    }

    private void Torch(float x,float z,float h)
    {
        Box(x,0.12f,z,0.55f,0.24f,0.55f,_stone[2]);
        Post(x,h*0.51f,z,0.14f,h,_iron);
        Post(x,0.28f,z,0.28f,0.19f,_iron);
        Post(x,h*0.72f,z,0.19f,0.08f,_gold);
        Post(x,h,z,0.34f,0.115f,_iron);
        Post(x,h+0.22f,z,0.45f,0.06f,_iron);
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.Tau/6;
            var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            Vector3 basePos=new(x,h,z);
            Line(basePos+dir*0.14f,basePos+dir*0.25f+Vector3.Up*0.32f,0.04f,_iron);
        }
        Vector3 flame=new(x,h+0.32f,z);
        Place(_spire,_flame,flame,new(0.29f,0.52f,0.29f),new(0,0,-0.12f));
        Place(_spire,_flameCore,flame+new Vector3(0.035f,-0.07f,0.095f),new(0.13f,0.26f,0.13f));
        Lamp(flame+Vector3.Up*0.14f,new Color("ffa75c"),5.0f,4.8f);
        Motes(flame,_flame,13,1.24f,0.12f,false);
        Motes(flame+Vector3.Up*0.30f,Surface("5a5363"),7,1.8f,0.10f,true);
    }

    private void Motes(Vector3 point,StandardMaterial3D material,int count,float life,float radius,bool smoke,bool rune=false)
    {
        var mat=(StandardMaterial3D)material.Duplicate();
        mat.VertexColorUseAsAlbedo=true;
        mat.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        var color=smoke?new Color(1,1,1,0.23f):Colors.White;
        var gradient=new Gradient{Offsets=new[]{0f,0.2f,1f},Colors=new[]{new Color(1,1,1,0),color,new Color(1,1,1,0)}};
        Mesh mesh = smoke
            ? new SphereMesh { Radius=.5f, Height=1, RadialSegments=6, Rings=2, Material=mat }
            : GlyphParticles.Mesh(rune ? "rune" : "spark", material.AlbedoColor, 1);
        var emitter=new CpuParticles3D
        {
            Position=point,Amount=count,Lifetime=life,Preprocess=life,Mesh=mesh,ColorRamp=gradient,
            EmissionShape=CpuParticles3D.EmissionShapeEnum.Box, EmissionBoxExtents=new(radius,0.06f,radius*0.26f),
            Direction=new(0.08f,1,0),Spread=smoke?17:20,Gravity=new(0.05f,0.095f,0),
            InitialVelocityMin=0.31f,InitialVelocityMax=0.72f,ScaleAmountMin=smoke?0.12f:0.070f,
            ScaleAmountMax=smoke?0.29f:0.16f,LocalCoords=true,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(emitter);
        _emitters.Add((emitter,count));
    }
}
