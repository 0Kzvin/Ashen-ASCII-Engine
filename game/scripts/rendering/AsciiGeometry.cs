using Godot;
using System;

public partial class AsciiGeometry : CompositorEffect
{
    public Texture2Drd Field { get; } = new();
    public volatile float Cell = 9;
    const int FieldWidth = 1024, FieldHeight = 768;
    RenderingDevice device;
    Rid texture, shader, sampler, pipeline;
    static readonly StringName Clustered = "forward_clustered", NormalRoughness = "normal_roughness";

    public AsciiGeometry()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
        AccessResolvedDepth = true;
        NeedsNormalRoughness = true;
    }

    public void Prepare()
    {
        var spirv = GD.Load<RDShaderFile>("res://shaders/ascii_geometry.glsl").GetSpirV();
        RenderingServer.CallOnRenderThread(Callable.From(() =>
        {
            device = RenderingServer.GetRenderingDevice();
            shader = device.ShaderCreateFromSpirV(spirv);
            if (!shader.IsValid) return;
            pipeline = device.ComputePipelineCreate(shader);
            sampler = device.SamplerCreate(new RDSamplerState());
            texture = device.TextureCreate(new RDTextureFormat {
                Width = FieldWidth, Height = FieldHeight, Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
                UsageBits = RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.SamplingBit
            }, new RDTextureView());
            Field.TextureRdRid = texture;
        }));
    }

    public override void _RenderCallback(int type, RenderData data)
    {
        if (!pipeline.IsValid || data.GetRenderSceneBuffers() is not RenderSceneBuffersRD buffers
            || !buffers.HasTexture(Clustered, NormalRoughness))
            return;
        var target = buffers.GetTargetSize();
        if (target.X <= 0 || target.Y <= 0) return;
        Rid depth = buffers.GetDepthLayer(0), normal = buffers.GetTexture(Clustered, NormalRoughness);
        if (!depth.IsValid || !normal.IsValid) return;
        var uniforms = UniformSetCacheRD.GetCache(shader, 0, new Godot.Collections.Array<RDUniform> {
            Sampled(0, depth), Sampled(1, normal), Uniform(2, RenderingDevice.UniformType.Image, texture)
        });
        float cell = Cell;
        float width = Mathf.Floor(cell + .5f), height = Mathf.Floor(cell * 1.36f + .5f);
        ReadOnlySpan<float> constants = [target.X, target.Y, width, height];
        var push = System.Runtime.InteropServices.MemoryMarshal.AsBytes(constants);
        long list = device.ComputeListBegin();
        device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, uniforms, 0);
        device.ComputeListSetPushConstant(list, push, (uint)push.Length);
        device.ComputeListDispatch(list,
            (uint)Mathf.CeilToInt(Mathf.Min(FieldWidth, Mathf.Ceil(target.X / width)) / 8),
            (uint)Mathf.CeilToInt(Mathf.Min(FieldHeight, Mathf.Ceil(target.Y / height)) / 8), 1);
        device.ComputeListEnd();
    }

    static RDUniform Uniform(int binding, RenderingDevice.UniformType type, Rid resource)
    {
        var uniform = new RDUniform { Binding = binding, UniformType = type };
        uniform.AddId(resource);
        return uniform;
    }

    RDUniform Sampled(int binding, Rid resource)
    {
        var uniform = Uniform(binding, RenderingDevice.UniformType.SamplerWithTexture, sampler);
        uniform.AddId(resource);
        return uniform;
    }

    public void Release()
    {
        Enabled = false;
        RenderingServer.CallOnRenderThread(Callable.From(() =>
        {
            Field.TextureRdRid = default;
            if (texture.IsValid) device.FreeRid(texture);
            if (sampler.IsValid) device.FreeRid(sampler);
            if (shader.IsValid) device.FreeRid(shader);
            pipeline = default;
        }));
    }
}
