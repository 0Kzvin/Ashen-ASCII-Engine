shader_type canvas_item;
render_mode unshaded;

uniform sampler2D scene_color : hint_screen_texture, repeat_disable, filter_linear;
uniform sampler2D glyph_atlas : repeat_disable, filter_linear;
uniform sampler2D coverage_map : repeat_disable, filter_nearest;
uniform sampler2D cell_field : hint_default_black, repeat_disable, filter_nearest;
uniform bool has_geometry = false;
uniform float cell_width = 9.0;
uniform float density = 1.0;
uniform float contrast = 1.0;
uniform float brightness = 1.0;
uniform float saturation = 1.0;
uniform float exposure = 1.0;
uniform float edge_strength = 1.0;
uniform float stability = 0.8;
uniform int palette_index = 0;
uniform int glyph_family = 0;

float luma(vec3 color) { return dot(color, vec3(0.2126, 0.7152, 0.0722)); }
float signal(vec3 color) { return mix(luma(color), max(color.r, max(color.g, color.b)), 0.32); }
vec3 scene(vec2 uv) { return textureLod(scene_color, uv, 0.0).rgb; }

int material_row(vec3 c, float roughness) {
    if (c.r > c.g * 1.55 && c.r > c.b * 1.32 && c.g < c.b * 1.2) return 7;
    if (c.b > c.g * 1.23 && c.r > c.g * 1.10) return 8;
    if (c.g > c.r * 1.49 && c.b > c.r * 1.40 && c.g > c.b * 0.78) return 8;
    if (c.r > c.b * 1.33 && c.r > c.g * 1.15 && c.g > c.b * 1.18) return 13;
    if (c.g > c.r * 1.10 && c.g > c.b * 1.11) return 10;
    if (c.b > c.r * 1.5 && c.b > c.g * 1.17 && c.g > c.r * 1.17) return 11;
    if (roughness > 0.0 && roughness < 0.63) return 12;
    return 9;
}

float glyph(float index, vec2 local, int row) {
    vec2 origin = vec2(index, float(row)) * 10.0;
    return textureLod(glyph_atlas, (origin + vec2(1.0) + local * 8.0) / vec2(textureSize(glyph_atlas, 0)), 0.0).r;
}

float oriented_glyph(vec2 grad, vec2 local, int row) {
    float ax = abs(grad.x), ay = abs(grad.y);
    float diagonal = grad.x * grad.y < 0.0 ? 17.0 : 19.0;
    float diagonal_mix = smoothstep(0.32, 0.65, min(ax, ay) / max(max(ax, ay), 0.001));
    return mix(glyph(ax > ay ? 18.0 : 16.0, local, row), glyph(diagonal, local, row), diagonal_mix);
}

void fragment() {
    vec2 cell_px = floor(vec2(cell_width, cell_width * 1.36) + 0.5);
    vec2 step_uv = cell_px * SCREEN_PIXEL_SIZE;
    vec2 grid = SCREEN_UV / step_uv;
    ivec2 id = ivec2(floor(grid));
    vec2 local = fract(grid);
    vec2 center = (vec2(id) + 0.5) * step_uv;
    vec2 offset = step_uv * mix(0.25, 0.43, stability);
    vec3 mid = scene(center);
    vec3 left = scene(center - vec2(offset.x, 0)), right = scene(center + vec2(offset.x, 0));
    vec3 top = scene(center - vec2(0, offset.y)), bottom = scene(center + vec2(0, offset.y));
    vec3 average = mix(mid, (left + right + top + bottom) * 0.25, mix(0.48, 0.78, stability));
    float level = signal(average) * exposure;
    float peak = max(max(signal(left), signal(right)), max(max(signal(top), signal(bottom)), signal(mid))) * exposure;
    float highlight = smoothstep(0.56, 0.94, peak);
    float glint = smoothstep(0.60, 0.96, peak) * smoothstep(0.16, 0.53, peak - level);
    float tone = clamp((level - 0.19) * contrast + 0.19, 0.0, 1.0);
    vec4 geometry = vec4(0.5, 0.5, 0.0, 0.0);
    if (has_geometry && all(lessThan(id, textureSize(cell_field, 0)))) {
        geometry = texelFetch(cell_field, id, 0);
    }
    vec2 depth_edge = geometry.rg * 2.0 - 1.0;
    vec2 color_edge = vec2(signal(right) - signal(left), signal(bottom) - signal(top));
    vec2 gradient = color_edge * (2.0 * exposure) + depth_edge * 0.54;
    gradient.y /= 1.36;
    float edge = length(color_edge) * exposure + length(depth_edge) * 0.22 + geometry.b * 0.12;
    float contours = smoothstep(0.085, 0.27, edge * edge_strength) * smoothstep(0.045, 0.21, peak);
    int row = glyph_family != 0 ? glyph_family : material_row(average, geometry.a);
    float coverage = pow(max(tone - 0.064, 0.0) / 0.936, 1.08) * 0.56 * density;
    // Masonry on broad planes needs fewer marks than armour or emissive features.
    coverage *= row == 9 ? 0.63 + 0.37 * contours : 1.0;
    coverage += highlight * 0.065;
    int bin = int(clamp(coverage * 255.0 + 0.5, 0.0, 255.0));
    vec3 mapping = texelFetch(coverage_map, ivec2(bin, row), 0).rgb;
    float lower = round(mapping.r * 255.0), upper = round(mapping.g * 255.0);
    float pattern = fract(dot(vec2(id & ivec2(3)), vec2(0.25, 0.625)));
    float threshold = 0.19 + pattern * 0.66;
    float transition = 0.065 + stability * 0.12;
    float weight = smoothstep(threshold - transition, threshold + transition, mapping.b);
    float fill = mix(glyph(lower, local, row), glyph(upper, local, row), weight);
    float ink_mask = max(fill, oriented_glyph(gradient, local, row) * contours * 0.88);

    vec3 shadow = vec3(0.013, 0.021, 0.034), neutral = vec3(0.78, 0.80, 0.72);
    if (palette_index == 1) { shadow = vec3(0.010, 0.022, 0.047); neutral = vec3(0.65, 0.79, 0.95); }
    if (palette_index == 2) { shadow = vec3(0.031, 0.015, 0.017); neutral = vec3(0.93, 0.66, 0.46); }
    vec3 chroma = mix(vec3(luma(average)), average, saturation) * exposure;
    float metal_fill = row == 12 ? 0.23 : 0.0;
    vec3 ink = mix(chroma * (1.30 + 0.58 * tone), neutral * (0.30 + metal_fill + 0.70 * tone), 0.21);
    ink += neutral * contours * 0.17 + chroma * highlight * 0.34;
    vec3 peak_color = max(mid, max(max(left, right), max(top, bottom)));
    ink = mix(ink, mix(peak_color, vec3(luma(peak_color)), 1.0 - min(saturation, 1.0)), glint * 0.44);
    vec3 fine = scene(SCREEN_UV);
    vec3 undercoat = max(mix(vec3(luma(fine)), fine, saturation), vec3(0.0)) * exposure;
    float detail = smoothstep(0.19, 0.78, peak);
    // Keep a restrained undercoat and reserve local halation for true highlights.
    vec3 background = shadow + undercoat * (0.09 + 0.12 * detail) + chroma * highlight * 0.036;
    float visibility = smoothstep(0.053, 0.17, level + contours * 0.075 + highlight * 0.09);
    vec2 screen = SCREEN_UV * 2.0 - 1.0;
    float vignette = 1.0 - 0.29 * smoothstep(0.47, 1.65, dot(screen, screen));
    COLOR = vec4(clamp(mix(background, ink, ink_mask * visibility) * brightness * vignette, 0.0, 1.0), 1.0);
}
