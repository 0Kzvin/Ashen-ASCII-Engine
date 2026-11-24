shader_type spatial;

uniform sampler2D glyph_atlas : repeat_disable, filter_linear;
uniform vec4 paint : source_color = vec4(1.0);
uniform int family = 0;
uniform float glyph_width = 0.071;
uniform float emission = 0.0;
uniform float surface_tag = 0.185;
varying vec3 local_point;
varying vec3 local_normal;

void vertex() {
    local_point = VERTEX;
    local_normal = NORMAL;
}

float cell_hash(vec2 cell) {
    return fract(dot(mod(cell, 19.0), vec2(0.277, 0.619)));
}

float engraving(vec2 at, float orientation) {
    vec2 grid = vec2(at.x, at.y / 1.19) / glyph_width + vec2(0.5, 0.28);
    vec2 cell = floor(grid);
    vec2 paper = fract(grid);
    float h = cell_hash(cell);
    float glyph = 1.0 + floor(h * 11.0);
    if (family == 0) {
        // Rails and diagonal hatching follow the axes of the armour plate.
        glyph = h > 0.70 ? (orientation < 0.0 ? 2.0 : 1.0) : 3.0 + floor(h * 12.0);
        if (mod(cell.y, 5.0) == 1.0) glyph = 6.0 + mod(cell.x + 16.0, 2.0);
    }
    if (family == 1) {
        glyph = h < 0.17 ? 0.0 : 2.0 + floor(h * 11.0);
        if (mod(cell.y, 4.0) == 0.0) glyph = 3.0 + mod(abs(cell.x), 3.0);
    }
    if (family == 2) glyph = 1.0 + floor(h * 13.0);
    vec2 position = (vec2(glyph, float(family)) * 10.0 + 1.0 + (paper - 0.5) * 8.0 + 4.0) / vec2(160.0, 80.0);
    float ink = texture(glyph_atlas, position).r;
    // Fade a grazing projection before it falls below a pixel.
    float pixels = 1.0 / max(max(fwidth(grid.x), fwidth(grid.y)), 0.001);
    return mix(0.17, ink, smoothstep(1.8, 4.5, pixels));
}

void fragment() {
    vec3 direction = normalize(local_normal);
    vec3 axis = abs(direction);
    vec2 coordinates = local_point.xy;
    if (axis.x > max(axis.y, axis.z)) coordinates = local_point.zy * vec2(-sign(direction.x), 1.0);
    if (axis.y > max(axis.x, axis.z)) coordinates = local_point.xz;
    coordinates.y *= -1.0;
    float glyph = engraving(coordinates, sign(direction.x + 0.001));
    float metal = family == 0 ? 1.0 : 0.0;
    float hot = family == 2 ? 1.0 : 0.0;
    float rim = pow(1.0 - clamp(abs(dot(normalize(NORMAL), normalize(VIEW))), 0.0, 1.0), 3.1);
    ALBEDO = paint.rgb * (0.018 + glyph * 0.91);
    EMISSION = paint.rgb * (0.008 + glyph * (0.13 + emission))
        + vec3(0.24, 0.10, 0.018) * hot * glyph
        + vec3(0.048, 0.09, 0.15) * metal * rim * glyph;
    METALLIC = 0.18 * metal;
    SPECULAR = 0.13 * glyph * metal;
    ROUGHNESS = surface_tag;
}
