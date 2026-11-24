shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never;

uniform sampler2D glyph_atlas : repeat_disable, filter_linear;

void fragment() {
    // U follows the history of the swing. V runs from the hilt toward the edge.
    vec2 grid = UV * vec2(14.0, 5.0);
    float choice = fract(dot(floor(grid), vec2(0.37, 0.63)));
    float col = 1.0 + floor(choice * 11.0);
    vec2 tile = (vec2(col, 2.0)*10.0 + 1.0 + fract(grid)*8.0) / vec2(160.0,80.0);
    float glyph = texture(glyph_atlas, tile).r;
    float rim = pow(clamp(UV.y,0.0,1.0), 3.2);
    ALBEDO = mix(vec3(0.96,0.30,0.05),vec3(1.0,0.77,0.34),rim);
    ALPHA = COLOR.a * (glyph * 0.82 + rim * 0.12);
}
