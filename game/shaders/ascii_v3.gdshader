shader_type canvas_item;
render_mode unshaded;

uniform sampler2D scene_color : hint_screen_texture, repeat_disable, filter_linear;
uniform sampler2D glyph_atlas : repeat_disable, filter_linear;
uniform sampler2D cell_field : hint_default_black, repeat_disable, filter_nearest;
uniform sampler2D surface_mask : hint_default_black, repeat_disable, filter_linear;
uniform bool has_geometry = false;
uniform bool preserve_material_ink = true;
uniform float cell_width = 10.0;
uniform float density = 1.0;
uniform float contrast = 1.0;
uniform float brightness = 1.0;
uniform float saturation = 1.0;
uniform float exposure = 1.0;
uniform float edge_strength = 1.0;
uniform float stability = 0.8;
uniform int palette_index = 0;
uniform int glyph_family = 0;

float luma(vec3 c) { return dot(c, vec3(0.2126, 0.7152, 0.0722)); }
float value(vec3 c) { return mix(luma(c), max(c.r, max(c.g, c.b)), 0.33); }
vec3 scene(vec2 uv) { return textureLod(scene_color, uv, 0.0).rgb; }

int family_for(vec3 c, float surface) {
    if (c.r > c.g * 1.40 && c.r > c.b * 1.36 && c.g < c.b * 1.23) return 1;
    if (c.r > c.b * 1.53 && c.g > c.b * 1.32 && c.r > c.g * 1.06) return 2;
    if (c.g > c.r * 1.09 && c.g > c.b * 1.10) return 4;
    if ((c.b > c.g * 1.22 && c.r > c.g * 1.05) || (c.g > c.r * 1.48 && c.b > c.r * 1.35)) return 5;
    if (c.b > c.r * 1.52 && c.b > c.g * 1.16) return 6;
    if (surface > 0.0 && surface < 0.62) return 0;
    return 3;
}

float stamp(float col, int row, vec2 p) {
    vec2 letter = (p - 0.5) * vec2(1.0, 1.30) + 0.5;
    vec2 atlas = vec2(col, float(row)) * 10.0 + 1.0 + clamp(letter, vec2(-0.06), vec2(1.06)) * 8.0;
    return textureLod(glyph_atlas, atlas / vec2(160.0, 80.0), 0.0).r;
}

float contour(vec2 grad, vec2 p) {
    vec2 axis = abs(grad);
    float align = min(axis.x, axis.y) / max(max(axis.x, axis.y), 0.0001);
    float tangent = axis.x > axis.y ? 2.0 : 1.0;
    float diagonal = grad.x * grad.y < 0.0 ? 4.0 : 3.0;
    return stamp(align > 0.47 ? diagonal : tangent, 7, p);
}

void fragment() {
    vec2 px = floor(vec2(cell_width, cell_width * 1.30) + 0.5);
    vec2 step_uv = px * SCREEN_PIXEL_SIZE;
    vec2 grid = SCREEN_UV / step_uv;
    ivec2 id = ivec2(floor(grid));
    vec2 center = (vec2(id) + 0.5) * step_uv;
    vec2 p = fract(grid);
    vec2 span = step_uv * mix(0.23, 0.43, stability);
    vec3 mid = scene(center);
    vec3 l = scene(center - vec2(span.x, 0)), r = scene(center + vec2(span.x, 0));
    vec3 u = scene(center - vec2(0, span.y)), d = scene(center + vec2(0, span.y));
    vec3 average = mix(mid, 0.25 * (l+r+u+d), mix(0.52, 0.77, stability));
    vec3 fine = scene(SCREEN_UV);
    vec4 geo = vec4(0.5, 0.5, 0.0, 0.0);
    if (has_geometry && all(lessThan(id, textureSize(cell_field, 0)))) geo = texelFetch(cell_field, id, 0);
    vec2 shape = geo.rg * 2.0 - 1.0;
    vec2 tone_gradient = vec2(value(r)-value(l), value(d)-value(u));
    vec2 gradient = tone_gradient * 1.5 + shape * 0.34;
    gradient.y /= 1.30;

    float level = value(average) * exposure;
    float peak = max(max(value(l), value(r)), max(max(value(u), value(d)), value(mid))) * exposure;
    int row = glyph_family == 0 ? family_for(average, geo.a) : glyph_family - 1;
    vec2 mask = textureLod(surface_mask, SCREEN_UV, 0.0).rg;
    float floor_mask = textureLod(surface_mask, center, 0.0).g;
    float importance = row == 3 ? 0.72 : 1.0;
    importance *= mix(1.0, 0.16, floor_mask);
    float edge = length(tone_gradient) * exposure + length(shape) * 0.16 + geo.b * 0.075;
    float e = smoothstep(0.105, 0.31, edge * edge_strength) * importance * smoothstep(0.06, 0.28, peak);
    float tone = clamp((level - 0.20) * contrast + 0.20, 0.0, 1.0);
    float noise = fract(dot(mod(vec2(id), 11.0), vec2(0.371, 0.693)));
    float coverage = smoothstep(0.10, 0.85, tone) * importance * density;
    float open = smoothstep(0.045, 0.23, tone) * smoothstep(noise * 0.45, noise * 0.45 + 0.18, coverage + e * 0.26);
    open *= mix(1.0, 0.27 + step(0.91, noise) * 0.34, floor_mask);
    float col = clamp(floor(2.0 + coverage * 10.0 + noise * 3.0), 1.0, 15.0);
    float glyph = max(stamp(col, row, p) * open * (1.0 - e * 0.78), contour(gradient, p) * e);

    vec3 paper = vec3(0.007, 0.011, 0.018), neutral = vec3(0.78, 0.82, 0.83);
    if (palette_index == 1) { paper = vec3(0.006, 0.012, 0.025); neutral = vec3(0.56, 0.74, 0.96); }
    if (palette_index == 2) { paper = vec3(0.021, 0.010, 0.012); neutral = vec3(0.93, 0.68, 0.48); }
    vec3 graded = max(mix(vec3(luma(fine)), fine, saturation), vec3(0.0)) * exposure;
    vec3 chroma = max(mix(vec3(luma(average)), average, saturation), vec3(0.0)) * exposure;
    vec3 ink = chroma * 1.52 + neutral * (0.08 + e * 0.16);
    vec3 undercoat = paper + graded * mix(0.145, 0.11, mask.g);
    // Preserve narrow luminous marks independently of the fill grid.
    float spark = smoothstep(0.43, 0.82, value(graded)) * smoothstep(0.19, 0.44, peak - level);
    vec3 painted = mix(undercoat, ink, clamp(glyph, 0.0, 1.0));
    painted = mix(painted, graded * 0.95 + paper, spark * 0.77);
    if (preserve_material_ink) {
        vec3 anchored = paper + graded * (1.02 + 0.08 * tone);
        painted = mix(painted, anchored, smoothstep(0.18, 0.80, mask.r));
    }
    vec2 frame = SCREEN_UV * 2.0 - 1.0;
    painted *= 1.0 - 0.19 * smoothstep(0.40, 1.8, dot(frame, frame));
    COLOR = vec4(clamp(painted * brightness, 0.0, 1.0), 1.0);
}
