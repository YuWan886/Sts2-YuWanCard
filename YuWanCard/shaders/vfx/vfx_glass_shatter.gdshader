shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

uniform float fall_progress : hint_range(0.0, 1.0) = 0.0;

uniform int pieces_x = 5;
uniform int pieces_y = 3;

uniform float gravity : hint_range(0.0, 4.0) = 2.2;
uniform float scatter : hint_range(0.0, 1.0) = 0.22;
uniform float rotation_amount : hint_range(0.0, 4.0) = 1.0;

uniform float point_jitter : hint_range(0.0, 0.48) = 0.34;
uniform float delay_variation : hint_range(0.0, 0.8) = 0.28;

uniform float edge_thickness : hint_range(0.0005, 0.05) = 0.01;
uniform float edge_brightness : hint_range(0.0, 2.0) = 0.35;
uniform float alpha_fade : hint_range(0.0, 1.0) = 0.0;

const int SEARCH_RADIUS = 1;

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

vec2 hash22(vec2 p) {
	return vec2(
		hash12(p + vec2(1.23, 4.56)),
		hash12(p + vec2(7.89, 0.12))
	);
}

mat2 rot(float a) {
	float s = sin(a);
	float c = cos(a);
	return mat2(vec2(c, -s), vec2(s, c));
}

vec2 cell_point(vec2 cell_id) {
	vec2 h = hash22(cell_id);
	return cell_id + 0.5 + (h - 0.5) * (point_jitter * 2.0);
}

void voronoi_info(vec2 p, out vec2 owner_cell, out vec2 owner_point, out float edge_dist) {
	vec2 base = floor(p);

	float best_d = 1e20;
	float second_d = 1e20;
	vec2 best_cell = vec2(0.0);
	vec2 best_point = vec2(0.0);

	for (int j = -SEARCH_RADIUS; j <= SEARCH_RADIUS; j++) {
		for (int i = -SEARCH_RADIUS; i <= SEARCH_RADIUS; i++) {
			vec2 c = base + vec2(float(i), float(j));
			vec2 pt = cell_point(c);
			float d = distance(p, pt);

			if (d < best_d) {
				second_d = best_d;
				best_d = d;
				best_cell = c;
				best_point = pt;
			} else if (d < second_d) {
				second_d = d;
			}
		}
	}

	owner_cell = best_cell;
	owner_point = best_point;
	edge_dist = second_d - best_d;
}

void piece_transform(
	vec2 piece_cell,
	out float t,
	out vec2 piece_offset_uv,
	out float piece_angle
) {
	float rnd  = hash12(piece_cell);
	float rnd2 = hash12(piece_cell + 17.37);
	float rnd3 = hash12(piece_cell + 91.11);

	float delay = rnd * delay_variation;
	t = clamp((fall_progress - delay) / max(0.0001, 1.0 - delay), 0.0, 1.0);

	float fall_y = gravity * t * t * (1.0 + rnd2 * 0.9);
	float drift_x = (rnd2 - 0.5) * scatter * t;
	float drift_y = (rnd3 - 0.5) * scatter * 0.18 * t;

	piece_offset_uv = vec2(drift_x, fall_y + drift_y);
	piece_angle = (rnd - 0.5) * rotation_amount * t;
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 grid = vec2(float(pieces_x), float(pieces_y));

	vec2 p_now = uv * grid;

	vec2 now_cell;
	vec2 now_point;
	float now_edge;
	voronoi_info(p_now, now_cell, now_point, now_edge);

	float t;
	vec2 piece_offset_uv;
	float piece_angle;
	piece_transform(now_cell, t, piece_offset_uv, piece_angle);

	vec2 piece_center_uv = now_point / grid;

	vec2 rel_now = uv - piece_center_uv;
	vec2 uv_from = piece_center_uv + rot(-piece_angle) * (rel_now - piece_offset_uv);

	float inside_screen =
		step(0.0, uv_from.x) *
		step(0.0, uv_from.y) *
		step(uv_from.x, 1.0) *
		step(uv_from.y, 1.0);

	vec2 safe_uv = clamp(uv_from, vec2(0.0), vec2(1.0));

	vec2 p_from = safe_uv * grid;
	vec2 from_cell;
	vec2 from_point;
	float from_edge;
	voronoi_info(p_from, from_cell, from_point, from_edge);

	float same_piece = 1.0 - step(0.001, distance(from_cell, now_cell));

	vec4 col = texture(screen_tex, safe_uv);

	float edge = 1.0 - smoothstep(0.0, edge_thickness, now_edge);
	col.rgb += edge * edge_brightness;

	col.a *= inside_screen;
	col.a *= same_piece;
	col.a *= mix(1.0, 1.0 - alpha_fade, t);

	COLOR = col;
}
