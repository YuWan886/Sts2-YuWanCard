shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

uniform float glitch_intensity : hint_range(0.0, 1.0) = 0.0;
uniform float time : hint_range(0.0, 10.0) = 0.0;

uniform float rgb_shift_amount : hint_range(0.0, 0.05) = 0.015;
uniform float scanline_intensity : hint_range(0.0, 1.0) = 0.15;
uniform float noise_intensity : hint_range(0.0, 1.0) = 0.08;
uniform float block_glitch_intensity : hint_range(0.0, 1.0) = 0.3;
uniform float distortion_amount : hint_range(0.0, 0.1) = 0.02;

float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

float random2(vec2 st) {
    return fract(sin(dot(st.xy, vec2(45.234, 98.432))) * 23421.631);
}

vec2 block_offset(vec2 uv, float time_val, float intensity) {
    vec2 block_uv = floor(uv * vec2(20.0, 15.0));
    float block_random = random(block_uv + floor(time_val * 8.0));
    
    if (block_random > 0.95) {
        float offset = (random2(block_uv + time_val) - 0.5) * intensity;
        return vec2(offset, 0.0);
    }
    return vec2(0.0);
}

void fragment() {
    vec2 uv = SCREEN_UV;
    
    float intensity = glitch_intensity;
    
    vec4 col = texture(screen_tex, uv);
    
    if (intensity > 0.001) {
        vec2 block_offset_val = block_offset(uv, time, block_glitch_intensity * intensity * 0.05);
        vec2 distorted_uv = uv + block_offset_val;
        
        float wave_distortion = sin(distorted_uv.y * 50.0 + time * 10.0) * distortion_amount * intensity;
        distorted_uv.x += wave_distortion;
        
        float rgb_shift = rgb_shift_amount * intensity;
        float r_offset = sin(time * 15.0) * rgb_shift;
        float b_offset = cos(time * 12.0) * rgb_shift;
        
        vec2 uv_r = clamp(distorted_uv + vec2(r_offset, 0.0), vec2(0.0), vec2(1.0));
        vec2 uv_g = clamp(distorted_uv, vec2(0.0), vec2(1.0));
        vec2 uv_b = clamp(distorted_uv + vec2(b_offset, 0.0), vec2(0.0), vec2(1.0));
        
        col.r = texture(screen_tex, uv_r).r;
        col.g = texture(screen_tex, uv_g).g;
        col.b = texture(screen_tex, uv_b).b;
        col.a = texture(screen_tex, uv_g).a;
        
        float scanline = sin(uv.y * 800.0) * 0.5 + 0.5;
        scanline = pow(scanline, 1.5) * scanline_intensity * intensity;
        col.rgb -= scanline;
        
        float scanline2 = sin(uv.y * 400.0 + time * 5.0) * 0.5 + 0.5;
        scanline2 = pow(scanline2, 2.0) * scanline_intensity * 0.5 * intensity;
        col.rgb -= scanline2;
        
        float noise_val = random(uv + time) * noise_intensity * intensity;
        col.rgb += noise_val - noise_intensity * 0.5 * intensity;
        
        float block_random = random(floor(uv * vec2(30.0, 20.0)) + floor(time * 10.0));
        if (block_random > 0.97) {
            col.rgb += random2(floor(uv * vec2(30.0, 20.0)) + time) * 0.3 * intensity;
        }
        
        float flicker = random(vec2(floor(time * 30.0), 0.0));
        if (flicker > 0.97) {
            col.rgb *= 0.7 + random(vec2(time, time)) * 0.3;
        }
        
        float vignette = 1.0 - length((uv - 0.5) * 1.2);
        col.rgb *= mix(1.0, vignette, intensity * 0.3);
        
        col.rgb = mix(col.rgb, col.rgb * vec3(1.1, 0.9, 1.05), intensity * 0.2);
    }
    
    COLOR = col;
}
