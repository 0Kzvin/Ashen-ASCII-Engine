shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D glyph_atlas : repeat_disable, filter_linear;

void fragment() {
    vec2 p = (UV - vec2(0.50,0.45)) * vec2(1.0,1.24);
    float r = length(p);
    float a = atan(p.y,p.x + 0.00001);
    float t = TIME * 0.20;
    vec2 along = vec2((a / 6.283185 + 0.5) * 39.0, (r - 0.31) * 48.0);
    float band = 1.0 - smoothstep(0.010,0.034,abs(r - 0.313));
    float inner = exp(-abs(r - 0.263)*145.0);
    float phase = fract(floor(along.x)*0.382 + 0.24);
    vec2 tile = (vec2(2.0 + floor(phase*13.0),5.0)*10.0 + 1.0 + vec2(fract(along.x), clamp(along.y,0.0,1.0))*8.0)/vec2(160.0,80.0);
    float glyph = texture(glyph_atlas,tile).r * step(0.0,along.y) * (1.0-step(1.0,along.y));
    float sweep = 0.57 + 0.43 * pow(0.5+0.5*sin(a*3.0-t*2.0),2.0);
    float veil = pow(max(0.0,sin(a*5.0+r*37.0-t*2.0)),8.0) * (1.0-smoothstep(0.19,0.36,r)) * smoothstep(0.06,0.25,r);
    vec3 tint = mix(vec3(0.046,0.30,0.65),vec3(0.42,0.11,0.57),0.5+0.5*sin(a-t));
    vec3 dark = vec3(0.008,0.013,0.034);
    ALBEDO = dark + tint * (veil * 0.24 + band * 0.085 + inner * 0.57 + glyph * sweep * 2.05);
    ROUGHNESS = 0.232;
}
