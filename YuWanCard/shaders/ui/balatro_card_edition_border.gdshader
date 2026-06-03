shader_type canvas_item;

uniform vec4 color_a : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 color_b : source_color = vec4(0.7, 0.8, 1.0, 1.0);
uniform float border_width : hint_range(0.01, 0.2) = 0.06;
uniform float pulse_speed : hint_range(0.1, 4.0) = 1.6;
uniform int mode = 0;

void fragment() {
    vec2 uv = UV;
    float edge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
    float border = 1.0 - smoothstep(border_width - 0.015, border_width + 0.015, edge);
    float center_mask = smoothstep(border_width * 1.2, border_width * 1.55, edge);
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed + uv.x * 12.0 + uv.y * 8.0);

    vec3 color = mix(color_a.rgb, color_b.rgb, pulse);
    if (mode == 1) {
        color = mix(color_a.rgb, color_b.rgb, uv.x);
    } else if (mode == 2) {
        color = vec3(
            0.55 + 0.45 * sin(TIME * pulse_speed + uv.x * 14.0),
            0.55 + 0.45 * sin(TIME * pulse_speed + uv.y * 15.0 + 2.1),
            0.55 + 0.45 * sin(TIME * pulse_speed + (uv.x + uv.y) * 11.0 + 4.2)
        );
    } else if (mode == 3) {
        color = mix(color_b.rgb, color_a.rgb, 1.0 - pulse);
        color *= vec3(0.85, 0.7, 1.1);
    }

    float alpha = border * (1.0 - center_mask) * 0.95;
    COLOR = vec4(color, alpha);
}
