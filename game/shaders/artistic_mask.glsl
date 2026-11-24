#[compute]
#version 450
layout(local_size_x = 8, local_size_y = 8) in;
layout(set = 0, binding = 0) uniform sampler2D depth_buffer;
layout(set = 0, binding = 1) uniform sampler2D normals;
layout(rg8, set = 0, binding = 2) uniform writeonly image2D masks;

void main() {
    ivec2 id = ivec2(gl_GlobalInvocationID.xy);
    ivec2 size = imageSize(masks);
    if (any(greaterThanEqual(id, size))) return;
    vec2 uv = (vec2(id) + 0.5) / vec2(size);
    float depth = textureLod(depth_buffer, uv, 0.0).r;
    float alpha = textureLod(normals, uv, 0.0).a;
    float roughness = min(alpha, 1.0 - alpha) * (255.0 / 127.0);
    // Authored materials reserve roughness bands for surface ink and paving.
    float present = step(0.00001, depth);
    float ink = step(0.168, roughness) * (1.0 - step(0.247, roughness));
    float paving = step(0.972, roughness);
    imageStore(masks, id, vec4(vec2(ink, paving) * present, 0.0, 0.0));
}
