shader_type spatial;
render_mode unshaded, cull_disabled, shadows_disabled;

uniform sampler2D glyph_atlas : repeat_disable, filter_linear;
uniform float phase = 0.0;

void vertex() {
    vec3 forward = CAMERA_POSITION_WORLD - MODEL_MATRIX[3].xyz;
    forward.y = 0.0;
    forward = normalize(forward);
    vec3 right = cross(vec3(0.0, 1.0, 0.0), forward);
    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(vec4(right,0.0), vec4(0.0,1.0,0.0,0.0), vec4(forward,0.0), MODEL_MATRIX[3]);
}

void fragment() {
    float h = 1.0 - UV.y;
    float t = TIME * 1.6 + phase;
    float lean = sin(h * 5.5 - t) * (0.038 + h * 0.16) + 0.11 * h;
    float contour = smoothstep(0.0, 0.22, h) * pow(max(1.0 - h, 0.0), 0.79) * 0.52;
    float body = abs(UV.x - 0.49 - lean);
    vec2 q = vec2(UV.x * 5.0, UV.y * 11.0 + t * 1.45);
    vec2 ij = floor(q);
    float hsh = fract(dot(mod(ij, 19.0), vec2(0.37, 0.679)));
    float col = 2.0 + floor(hsh * 10.0);
    vec2 tile = (vec2(col, 2.0)*10.0 + 1.0 + fract(q)*8.0) / vec2(160.0,80.0);
    float stamp = texture(glyph_atlas, tile).r;
    float taper = (1.0 - smoothstep(contour - 0.036, contour + 0.018, body));
    if (taper * stamp < 0.28 || h < 0.04) discard;
    float hot = pow(1.0 - clamp(h * 0.66 + body * 1.43,0.0,1.0), 2.6);
    vec3 copper = vec3(0.91, 0.13, 0.025);
    vec3 gold = vec3(1.26, 0.75, 0.28);
    ALBEDO = mix(copper, gold, hot) * (0.66 + stamp * 0.48);
    ROUGHNESS = 0.210;
}
