shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 1.0;
uniform float time : hint_range(0.0, 10.0) = 0.0;
uniform float scale : hint_range(0.1, 3.0) = 1.0;

float hash(float n) {
    return fract(sin(n) * 43758.5453123);
}

float hash2(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453123);
}

float zigzag_lightning(vec2 uv, vec2 start, vec2 end, float width, float seed, float time_val) {
    float result = 0.0;
    float dist = length(end - start);
    
    if (dist < 0.001) return 0.0;
    
    vec2 dir = normalize(end - start);
    vec2 perp = vec2(-dir.y, dir.x);
    
    vec2 local_uv = uv - start;
    float proj = dot(local_uv, dir);
    float perp_dist = abs(dot(local_uv, perp));
    
    if (proj < -0.05 || proj > dist + 0.05) return 0.0;
    
    float segments = 8.0;
    float prev_offset = 0.0;
    
    for (float i = 0.0; i < segments; i++) {
        float t1 = i / segments;
        float t2 = (i + 1.0) / segments;
        
        float offset1 = prev_offset;
        float offset2 = (hash(seed + i * 7.3 + time_val * 0.5) - 0.5) * 0.2 * dist;
        offset2 += sin(time_val * 25.0 + i * 3.0) * 0.03 * dist;
        
        prev_offset = offset2;
        
        float seg_start = t1 * dist;
        float seg_end = t2 * dist;
        
        if (proj >= seg_start && proj <= seg_end) {
            float local_t = (proj - seg_start) / (seg_end - seg_start);
            float current_offset = mix(offset1, offset2, local_t);
            float dist_to_line = abs(perp_dist - current_offset);
            float local_width = width * (1.0 - t1 * 0.2);
            float line_intensity = 1.0 - smoothstep(0.0, local_width, dist_to_line);
            result = max(result, line_intensity);
        }
        
        if (hash(seed + i * 13.7) > 0.6 && i > 1.0 && i < segments - 1.0) {
            float branch_proj = seg_start;
            float branch_offset = mix(offset1, offset2, 0.5);
            vec2 branch_start_pos = start + dir * branch_proj + perp * branch_offset;
            
            float branch_angle = (hash(seed + i * 5.1) - 0.5) * 1.5;
            vec2 branch_dir = normalize(dir + perp * tan(branch_angle));
            float branch_len = dist * 0.35 * hash(seed + i * 9.3);
            
            vec2 to_branch = uv - branch_start_pos;
            float branch_proj_local = dot(to_branch, branch_dir);
            float branch_perp = abs(dot(to_branch, vec2(-branch_dir.y, branch_dir.x)));
            
            if (branch_proj_local > 0.0 && branch_proj_local < branch_len) {
                float branch_dist = branch_perp;
                float branch_width = width * 0.6;
                float branch_intensity = 1.0 - smoothstep(0.0, branch_width, branch_dist);
                branch_intensity *= 1.0 - branch_proj_local / branch_len;
                result = max(result, branch_intensity * 0.7);
            }
        }
    }
    
    return result;
}

void fragment() {
    vec2 uv = UV;
    vec2 center = vec2(0.5, 0.5);
    vec2 to_center = uv - center;
    float dist = length(to_center);
    float angle = atan(to_center.y, to_center.x);
    
    float expansion = min(1.0, time * 4.0);
    float fade_out = 1.0 - smoothstep(0.6, 0.8, time);
    
    float lightning = 0.0;
    float core_intensity = 0.0;
    
    // 10条扭曲折线闪电从中心爆发
    for (int i = 0; i < 10; i++) {
        float fi = float(i);
        float base_angle = fi * 0.6283;
        float angle_jitter = hash(fi * 3.7 + time * 0.3) * 0.3 - 0.15;
        float bolt_angle = base_angle + angle_jitter;
        
        float max_len = 0.45 + hash(fi * 7.1) * 0.25;
        float bolt_len = max_len * expansion * scale;
        vec2 end = center + vec2(cos(bolt_angle), sin(bolt_angle)) * bolt_len;
        
        float bolt = zigzag_lightning(uv, center, end, 0.025 * scale, fi * 17.3, time);
        lightning = max(lightning, bolt);
        
        if (dist < 0.15 * scale) {
            core_intensity = max(core_intensity, bolt * 1.8);
        }
    }
    
    // 中心核心光晕
    float core_glow = exp(-dist * 10.0 / scale) * expansion;
    lightning = max(lightning, core_glow * 2.0);
    
    // 颜色层次 - 更亮的颜色
    vec3 dark_purple = vec3(0.15, 0.0, 0.25);
    vec3 mid_purple = vec3(0.4, 0.1, 0.6);
    vec3 bright_purple = vec3(0.7, 0.4, 1.0);
    vec3 white = vec3(0.95, 0.9, 1.0);
    
    vec3 lightning_col;
    float l = lightning;
    if (l < 0.3) {
        lightning_col = mix(dark_purple, mid_purple, l / 0.3);
    } else if (l < 0.7) {
        lightning_col = mix(mid_purple, bright_purple, (l - 0.3) / 0.4);
    } else {
        lightning_col = mix(bright_purple, white, (l - 0.7) / 0.3);
    }
    
    // 核心区域更亮
    lightning_col = mix(lightning_col, white, core_intensity * 0.7);
    
    // 脉冲效果
    float pulse = sin(time * 30.0) * 0.2 + 0.8;
    lightning_col *= pulse;
    
    // 辉光效果
    float glow = exp(-dist * 5.0 / scale) * 0.35 * expansion;
    vec3 glow_col = mix(dark_purple, bright_purple, glow);
    
    // 组合效果
    vec3 col = vec3(0.0);
    col += lightning_col * lightning * intensity * fade_out * 1.5;
    col += glow_col * glow * intensity * fade_out;
    
    // 边缘辉光
    float edge_glow = smoothstep(0.1, 0.4, lightning) * smoothstep(0.9, 0.6, lightning);
    col += bright_purple * edge_glow * 0.6 * intensity * fade_out;
    
    // 闪烁效果
    float flicker = hash2(floor(uv * 80.0) + floor(time * 50.0));
    if (flicker > 0.94 && lightning > 0.3) {
        col *= 0.8 + hash(time + uv.x) * 0.4;
    }
    
    // Alpha 计算 - 确保可见
    float alpha = lightning * intensity * fade_out;
    alpha = max(alpha, glow * intensity * fade_out * 0.7);
    alpha = max(alpha, edge_glow * intensity * fade_out * 0.3);
    
    COLOR = vec4(col, alpha);
}
