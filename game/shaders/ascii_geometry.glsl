#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8) in;
layout(set = 0, binding = 0) uniform sampler2D depth_buffer;
layout(set = 0, binding = 1) uniform sampler2D normal_buffer;
layout(rgba8, set = 0, binding = 2) uniform writeonly image2D cell_field;
layout(push_constant, std430) uniform Grid {
    vec2 viewport_size;
    vec2 cell_size;
};

float depth_at(vec2 uv) {
    return textureLod(depth_buffer, uv, 0.0).r;
}

vec3 normal_at(vec2 uv) {
    return normalize(textureLod(normal_buffer, uv, 0.0).xyz * 2.0 - 1.0 + vec3(0.0001));
}

float depth_step(float first, float second, float center) {
    // Relative change in reverse-Z. Reject the gentle slope of an oblique plane.
    float bend = abs(first + second - 2.0 * center);
    return smoothstep(0.028, 0.18, bend / max(max(first, second), max(center, 0.00001)));
}

float crease(vec2 a, vec2 b, float za, float zb) {
    if (min(za, zb) < 0.00001) return 0.0;
    return smoothstep(0.075, 0.55, 1.0 - dot(normal_at(a), normal_at(b)));
}

void main() {
    ivec2 id = ivec2(gl_GlobalInvocationID.xy);
    if (any(greaterThanEqual(id, min(imageSize(cell_field), ivec2(ceil(viewport_size / cell_size)))))) return;
    vec2 center = (vec2(id) + 0.5) * cell_size / viewport_size;
    vec2 half_cell = 0.34 * cell_size / viewport_size;
    vec2 l = center - vec2(half_cell.x, 0), r = center + vec2(half_cell.x, 0);
    vec2 t = center - vec2(0, half_cell.y), b = center + vec2(0, half_cell.y);
    float z = depth_at(center);
    float dl = depth_at(l), dr = depth_at(r), dt = depth_at(t), db = depth_at(b);
    vec2 depth_gradient = vec2(depth_step(dl, dr, z) * sign(dr - dl), depth_step(dt, db, z) * sign(db - dt));
    vec2 normal_gradient = vec2(crease(l, r, dl, dr), crease(t, b, dt, db));
    vec2 signed_normal = normal_gradient * sign(vec2(
        normal_at(r).x - normal_at(l).x, normal_at(b).y - normal_at(t).y));
    vec2 gradient = clamp(depth_gradient + 0.27 * signed_normal, -1.0, 1.0);
    float surface = textureLod(normal_buffer, center, 0.0).a;
    float roughness = min(surface, 1.0 - surface) * (255.0 / 127.0);
    imageStore(cell_field, id, vec4(0.5 + 0.5 * gradient, length(normal_gradient) * 0.7,
        z > 0.00001 ? 0.10 + 0.90 * roughness : 0.0));
}
