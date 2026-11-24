shader_type canvas_item;
render_mode unshaded, blend_disabled;

uniform sampler2D scene_buffer : hint_screen_texture, repeat_disable, filter_linear;
uniform sampler2D glyph_atlas : repeat_disable, filter_linear;
uniform int display_mode = 3;
uniform int palette_index = 0;
uniform int glyph_family = 0;
uniform float cell_width = 7.0;
uniform float density = 1.0;
uniform float contrast = 1.0;
uniform float brightness = 1.0;
uniform float saturation = 1.0;
uniform float exposure = 1.0;
uniform float edge_strength = 1.0;
uniform float stability = 0.75;

float luminance(vec3 rgb) {
	return dot(rgb, vec3(0.2126, 0.7152, 0.0722));
}

float light_value(vec3 rgb) {
	// Keep strongly saturated fire and magic legible at equal luminance.
	return mix(luminance(rgb), max(rgb.r, max(rgb.g, rgb.b)), 0.18);
}

float bayer2(vec2 p) {
	return 2.0 * mod(p.x + p.y, 2.0) + p.y;
}

float ordered_threshold(vec2 cell_id) {
	float a = bayer2(mod(cell_id, 2.0));
	float b = bayer2(mod(floor(cell_id * 0.5), 2.0));
	return (4.0 * a + b + 0.5) / 16.0 - 0.5;
}

vec3 read_scene(vec2 uv) {
	return textureLod(scene_buffer, uv, 0.0).rgb;
}

int cinematic_family(vec3 color) {
	// Estimate the art-directed material palette from ungraded, cell-averaged albedo and lighting.
	if (color.r > color.b * 1.35 && color.r > color.g * 1.12) {
		if (color.g > color.b * 1.30) { return 6; } // amber and flame
		return 0; // crimson fabric
	}
	if (color.g > color.r * 1.11 && color.g > color.b * 1.08) { return 3; } // moss
	if (color.b > color.g * 1.15 && color.r > color.g * 1.07) { return 1; } // violet sigils
	if (color.g > color.r * 1.65 && color.b > color.r * 1.70 && color.g > color.b * 0.80) {
		return 1; // cyan magic
	}
	if (color.b > color.r * 1.42 && color.b > color.g * 1.10) { return 4; } // blue water
	float spread = max(color.r, max(color.g, color.b)) - min(color.r, min(color.g, color.b));
	if (spread < max(0.018, luminance(color) * 0.12) && luminance(color) > 0.23) { return 5; }
	return 2; // masonry and distant silhouettes
}

float stroke(float column, vec2 local, int family) {
	// The atlas has 16 density and 4 edge glyphs on each 10px-high row.
	vec2 atlas_px = vec2(textureSize(glyph_atlas, 0));
	vec2 origin = vec2(column, float(family)) * 10.0;
	return textureLod(glyph_atlas, (origin + vec2(1.0) + local * 8.0) / atlas_px, 0.0).r;
}

float tangent_column(vec2 gradient) {
	vec2 g = abs(gradient);
	if (g.x < g.y * 0.4142) { return 16.0; }
	if (g.y < g.x * 0.4142) { return 18.0; }
	return gradient.x * gradient.y < 0.0 ? 17.0 : 19.0;
}

vec3 palette_paper() {
	if (palette_index == 1) { return vec3(0.028, 0.051, 0.096); }
	if (palette_index == 2) { return vec3(0.064, 0.034, 0.031); }
	return vec3(0.025, 0.033, 0.044);
}

vec3 palette_ink() {
	if (palette_index == 1) { return vec3(0.71, 0.86, 1.00); }
	if (palette_index == 2) { return vec3(1.00, 0.71, 0.48); }
	return vec3(0.92, 0.89, 0.80);
}

void fragment() {
	vec2 cell_px = vec2(max(4.0, floor(cell_width + 0.5)),
			floor(cell_width * 1.36 + 0.5));
	vec2 grid = SCREEN_UV / (cell_px * SCREEN_PIXEL_SIZE);
	vec2 cell_id = floor(grid);
	vec2 center = (cell_id + 0.5) * cell_px * SCREEN_PIXEL_SIZE;
	vec2 offset = cell_px * SCREEN_PIXEL_SIZE * mix(0.34, 0.55, stability);
	vec3 middle = read_scene(center);
	vec3 west = read_scene(center - vec2(offset.x, 0.0));
	vec3 east = read_scene(center + vec2(offset.x, 0.0));
	vec3 north = read_scene(center - vec2(0.0, offset.y));
	vec3 south = read_scene(center + vec2(0.0, offset.y));
	vec3 average = mix(middle, (west + east + north + south) * 0.25, mix(0.38, 0.76, stability));
	float value = light_value(average) * exposure;
	float tone = clamp((value - 0.26) * contrast + 0.26, 0.0, 1.0);
	float level = pow(tone, 0.82) * 15.0 * density;
	float dither = ordered_threshold(cell_id) * mix(0.92, 0.36, stability);
	float column = clamp(floor(level + dither + 0.5), 0.0, 15.0);
	vec2 gradient = vec2(light_value(east) - light_value(west),
			(light_value(south) - light_value(north)) * cell_px.x / cell_px.y);
	float edge = length(gradient) * exposure * edge_strength;
	float orientation = tangent_column(gradient);
	float edge_mix = smoothstep(0.062, 0.18, edge);
	// A brief crossfade softens transitions between texture and silhouette.
	int family = glyph_family;
	if (display_mode == 3 && glyph_family == 0) {
		// Seven independently editable material rows follow the seven global families.
		family = 7 + cinematic_family(average);
	}
	float glyph = stroke(column, fract(grid), family);
	if (edge_mix > 0.0) {
		glyph = mix(glyph, stroke(orientation, fract(grid), family), edge_mix);
	}
	vec3 paper = palette_paper();
	vec3 ink = palette_ink();
	vec3 result;
	if (display_mode == 4) {
		vec3 direction_color = vec3(0.97, 0.57, 0.17);
		if (orientation == 17.0) { direction_color = vec3(0.64, 0.39, 1.0); }
		if (orientation == 18.0) { direction_color = vec3(0.18, 0.91, 0.86); }
		if (orientation == 19.0) { direction_color = vec3(0.97, 0.36, 0.62); }
		result = mix(vec3(0.018 + tone * 0.18), mix(vec3(0.32 + tone * 0.65), direction_color, edge_mix), glyph);
	} else if (display_mode == 1) {
		result = mix(paper * (0.38 + tone * 0.32), ink * (0.22 + 0.88 * sqrt(tone) + 0.16 * edge_mix), glyph);
	} else {
		vec3 chroma = mix(vec3(luminance(average)), average, saturation) * exposure;
		vec3 colored_ink = clamp(max(chroma, vec3(0.0)) * 1.62 + ink * (0.10 + 0.15 * tone + 0.09 * edge_mix), vec3(0.0), vec3(1.0));
		if (display_mode == 2) {
			result = mix(paper * 0.42 + chroma * 0.12, colored_ink, glyph);
		} else {
			// A low-key continuous underpainting preserves fog, albedo and fine silhouettes.
			vec3 fine = read_scene(SCREEN_UV);
			fine = mix(vec3(luminance(fine)), fine, saturation) * exposure;
			vec3 base = max(mix(chroma, fine, 0.62), vec3(0.0));
			vec3 underpainting = base * (0.49 + 0.18 * tone) + paper * (0.36 - 0.20 * tone);
			vec3 filament = colored_ink * mix(vec3(1.0), ink, 0.14);
			result = mix(underpainting, filament, glyph * 0.95);
			vec2 screen = SCREEN_UV * 2.0 - 1.0;
			result *= 1.0 - 0.11 * smoothstep(0.30, 1.65, dot(screen, screen));
		}
	}
	COLOR = vec4(clamp(result * brightness, vec3(0.0), vec3(1.0)), 1.0);
}
