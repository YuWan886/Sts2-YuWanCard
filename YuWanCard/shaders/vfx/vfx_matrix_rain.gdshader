shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

uniform float time : hint_range(0.0, 30.0) = 0.0;
uniform float intensity : hint_range(0.0, 1.0) = 1.0;
uniform float fade : hint_range(0.0, 1.0) = 1.0;

uniform float column_speed : hint_range(0.5, 5.0) = 2.5;
uniform float column_density : hint_range(5.0, 150.0) = 35.0;
uniform float char_size : hint_range(0.5, 50.0) = 20.0;
uniform float trail_length : hint_range(5.0, 40.0) = 12.0;

uniform vec3 lead_color : source_color = vec3(0.9, 1.0, 0.9);
uniform vec3 trail_color : source_color = vec3(0.1, 0.8, 0.2);
uniform vec3 glow_color : source_color = vec3(0.0, 0.4, 0.1);

uniform float glitch_chance : hint_range(0.0, 1.0) = 0.015;
uniform float scanline_intensity : hint_range(0.0, 0.5) = 0.08;

float hash(float n) {
    return fract(sin(n) * 43758.5453123);
}

float hash2(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453123);
}

float hash3(vec3 p) {
    return fract(sin(dot(p, vec3(12.9898, 78.233, 45.164))) * 43758.5453123);
}

float get_char_pattern(float char_index, vec2 local_uv) {
    float pattern = 0.0;
    
    float ci = floor(char_index);
    
    float row1 = hash(ci * 1.1 + 0.1);
    float row2 = hash(ci * 1.2 + 0.2);
    float row3 = hash(ci * 1.3 + 0.3);
    float row4 = hash(ci * 1.4 + 0.4);
    float row5 = hash(ci * 1.5 + 0.5);
    
    float y = local_uv.y;
    float x = local_uv.x;
    
    float threshold = 0.45;
    
    if (y < 0.2) {
        pattern = step(threshold, row1);
    } else if (y < 0.4) {
        pattern = step(threshold, row2);
    } else if (y < 0.6) {
        pattern = step(threshold, row3);
    } else if (y < 0.8) {
        pattern = step(threshold, row4);
    } else {
        pattern = step(threshold, row5);
    }
    
    float edge_x = smoothstep(0.0, 0.15, x) * smoothstep(1.0, 0.85, x);
    float edge_y = smoothstep(0.0, 0.1, y) * smoothstep(1.0, 0.9, y);
    
    return pattern * edge_x * edge_y;
}

float get_alphanumeric_brightness(float char_id, float time_val) {
    float flicker = hash2(vec2(char_id, floor(time_val * 15.0)));
    float base_brightness = 0.7 + hash(char_id * 0.7) * 0.3;
    
    if (flicker > 0.92) {
        base_brightness *= 1.4;
    }
    
    float pulse = sin(char_id * 2.5 + time_val * 4.0) * 0.15 + 0.85;
    
    return base_brightness * pulse;
}

void fragment() {
    vec2 uv = SCREEN_UV;
    
    vec4 original_color = texture(screen_tex, uv);
    vec3 matrix_color = vec3(0.0);
    float total_alpha = 0.0;
    
    float num_columns = column_density;
    float column_width = 1.0 / num_columns;
    
    float col_idx = floor(uv.x * num_columns);
    float col_seed = hash(col_idx * 1.234 + 0.567);
    float col_speed_var = 0.6 + hash(col_idx * 2.345 + 1.234) * 0.8;
    
    float col_time = time * column_speed * col_speed_var;
    float col_y = fract(col_seed + col_time);
    float col_y_screen = col_y * 1.4 - 0.2;
    
    float dist_from_head = (col_y_screen - uv.y) * trail_length;
    
    if (dist_from_head >= 0.0 && dist_from_head <= trail_length * 0.75) {
        float head_brightness = exp(-dist_from_head * 0.6);
        float trail_fade = 1.0 - smoothstep(0.0, trail_length * 0.65, dist_from_head);
        float alpha = head_brightness * trail_fade;
        
        if (alpha > 0.02) {
            float char_y = floor(uv.y * char_size);
            float char_time_bucket = floor(time * 8.0);
            float char_id = char_y + col_idx * 50.0 + char_time_bucket * 0.1;
            
            float alphanumeric_id = floor(hash3(vec3(char_y, col_idx, char_time_bucket)) * 62.0);
            
            float char_brightness = get_alphanumeric_brightness(char_id, time);
            
            float local_char_x = fract(uv.x * num_columns);
            float local_char_y = fract(uv.y * char_size);
            vec2 local_uv = vec2(local_char_x, local_char_y);
            
            float char_pattern = get_char_pattern(alphanumeric_id, local_uv);
            
            float is_head = smoothstep(1.5, 0.0, dist_from_head);
            
            vec3 final_color = mix(trail_color, lead_color, is_head);
            final_color += glow_color * alpha * 0.25;
            
            float col_alpha = alpha * (1.0 - smoothstep(0.0, 0.4, abs(fract(uv.x * num_columns) - 0.5) * 2.0));
            
            matrix_color = final_color * char_brightness * char_pattern * col_alpha;
            total_alpha = col_alpha * 0.65;
        }
    }
    
    float glitch = hash2(floor(uv * vec2(80.0, 40.0)) + floor(time * 25.0));
    if (glitch > 1.0 - glitch_chance * intensity) {
        float glitch_intensity = hash2(uv + time) * 0.4;
        matrix_color += vec3(0.0, glitch_intensity, 0.0);
    }
    
    float scanline = sin(uv.y * 600.0) * 0.5 + 0.5;
    scanline = pow(scanline, 2.5) * scanline_intensity;
    matrix_color -= vec3(scanline * 0.25);
    
    float vignette = 1.0 - length((uv - 0.5) * 1.25);
    vignette = smoothstep(0.0, 1.0, vignette);
    matrix_color *= vignette;
    
    float screen_tint = 0.12 * intensity;
    vec3 tinted_original = mix(original_color.rgb, original_color.rgb * vec3(0.75, 1.0, 0.85), screen_tint);
    
    vec3 final_color = mix(tinted_original, matrix_color + tinted_original * 0.4, total_alpha * intensity * fade);
    final_color = mix(original_color.rgb, final_color, intensity * fade);
    
    COLOR = vec4(final_color, 1.0);
}
