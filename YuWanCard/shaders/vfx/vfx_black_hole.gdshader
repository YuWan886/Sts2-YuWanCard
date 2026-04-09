shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

uniform float time : hint_range(0.0, 10.0) = 0.0;
uniform float black_hole_radius : hint_range(0.0, 1.0) = 0.0;
uniform float rotation_speed : hint_range(0.0, 10.0) = 2.0;
uniform float distortion_strength : hint_range(0.0, 5.0) = 1.0;
uniform float blur_strength : hint_range(0.0, 2.0) = 0.5;
uniform float explosion_intensity : hint_range(0.0, 5.0) = 0.0;
uniform float suck_strength : hint_range(0.0, 5.0) = 1.0;

uniform vec3 core_color = vec3(0.02, 0.0, 0.05);
uniform vec3 inner_ring_color = vec3(0.9, 0.4, 1.0);
uniform vec3 mid_ring_color = vec3(0.6, 0.2, 0.9);
uniform vec3 outer_ring_color = vec3(0.2, 0.05, 0.4);
uniform vec3 glow_color = vec3(0.5, 0.15, 0.7);

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(
        mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
        u.y
    );
}

float fbm(vec2 p) {
    float v = 0.0;
    float a = 0.5;
    mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
    for (int i = 0; i < 4; i++) {
        v += a * noise(p);
        p = rot * p * 2.0;
        a *= 0.5;
    }
    return v;
}

vec2 gravitational_warp(vec2 uv, vec2 center, float radius, float strength, float suck) {
    vec2 delta = uv - center;
    float dist = length(delta);
    if (dist < 0.001) return uv;

    float falloff = radius * 6.0;
    if (dist > falloff) return uv;

    float t = dist / falloff;
    
    float warp = strength * (1.0 - t) * (1.0 - t) * 0.35;
    float suck_amount = suck * (1.0 - t) * (1.0 - t) * (1.0 - t) * 0.8;
    
    vec2 dir = normalize(delta);

    float angle = atan(delta.y, delta.x);
    float spin = strength * (1.0 - smoothstep(0.0, radius * 1.5, dist)) * rotation_speed * 0.8;
    float new_angle = angle + spin / max(dist, 0.01);

    float new_dist = dist * (1.0 - suck_amount);
    
    vec2 warped = center + vec2(cos(new_angle), sin(new_angle)) * new_dist;
    warped += dir * warp;

    return warped;
}

vec2 einstein_ring(vec2 uv, vec2 center, float radius, float strength) {
    vec2 delta = uv - center;
    float dist = length(delta);
    float ring_zone = radius * 2.5;

    if (dist > radius && dist < ring_zone) {
        float t = (dist - radius) / (ring_zone - radius);
        float lens = strength * sin(t * 3.14159) * 0.08 * (radius / max(dist, 0.001));
        return uv + normalize(delta) * lens;
    }
    return uv;
}

vec3 temperature_gradient(float t) {
    t = clamp(t, 0.0, 1.0);
    if (t < 0.33) {
        return mix(core_color, inner_ring_color, t / 0.33);
    } else if (t < 0.66) {
        return mix(inner_ring_color, mid_ring_color, (t - 0.33) / 0.33);
    } else {
        return mix(mid_ring_color, outer_ring_color, (t - 0.66) / 0.34);
    }
}

vec4 blur(sampler2D tex, vec2 uv, float radius, float dist, float max_dist) {
    vec4 result = vec4(0.0);
    float total = 0.0;
    
    float blur_amount = radius * (1.0 - smoothstep(max_dist * 0.3, max_dist, dist));
    vec2 tex_size = vec2(textureSize(tex, 0));
    vec2 pixel_size = 1.0 / tex_size;
    
    for (float x = -4.0; x <= 4.0; x += 1.0) {
        for (float y = -4.0; y <= 4.0; y += 1.0) {
            float weight = exp(-(x * x + y * y) / (2.0 * blur_amount * blur_amount));
            vec2 offset = vec2(x, y) * pixel_size * blur_amount;
            result += texture(tex, uv + offset) * weight;
            total += weight;
        }
    }
    
    return result / total;
}

void fragment() {
    vec2 uv = SCREEN_UV;
    vec2 center = vec2(0.5, 0.5);

    vec2 delta = uv - center;
    float dist = length(delta);
    float angle = atan(delta.y, delta.x);

    float r = black_hole_radius;
    float event_horizon = r * 0.25;
    float photon_sphere = r * 0.5;

    vec2 warped_uv = gravitational_warp(uv, center, r, distortion_strength, suck_strength);
    warped_uv = einstein_ring(warped_uv, center, r, distortion_strength);

    float max_blur_dist = r * 6.0;
    vec4 col;
    if (dist < max_blur_dist && blur_strength > 0.01) {
        col = blur(screen_tex, warped_uv, blur_strength * 3.0, dist, max_blur_dist);
    } else {
        col = texture(screen_tex, warped_uv);
    }

    float suck_zone = r * 4.0;
    if (dist < suck_zone && suck_strength > 0.01) {
        float darken_t = dist / suck_zone;
        float darken = (1.0 - darken_t) * suck_strength * 0.4;
        col.rgb *= (1.0 - darken);
        
        float stretch = (1.0 - darken_t) * suck_strength * 0.3;
        vec3 desaturate = vec3(dot(col.rgb, vec3(0.299, 0.587, 0.114)));
        col.rgb = mix(col.rgb, desaturate, stretch);
    }

    if (dist < event_horizon) {
        float edge_glow = smoothstep(event_horizon * 0.5, event_horizon, dist);
        vec3 horizon_col = mix(core_color, inner_ring_color * 0.3, edge_glow);
        float horizon_noise = fbm(uv * 30.0 + vec2(time * 2.0, 0.0)) * 0.1;
        col = vec4(horizon_col + horizon_noise, 1.0);
    }
    else if (dist < r) {
        float t = (dist - event_horizon) / (r - event_horizon);
        vec3 disk_color = temperature_gradient(1.0 - t);

        float spiral1 = fract(angle * 2.0 / 6.28318 + time * rotation_speed * 0.8);
        float spiral2 = fract(angle * 3.0 / 6.28318 - time * rotation_speed * 0.5);
        float arm1 = smoothstep(0.0, 0.15, spiral1) * (1.0 - smoothstep(0.6, 0.85, spiral1));
        float arm2 = smoothstep(0.0, 0.1, spiral2) * (1.0 - smoothstep(0.7, 0.9, spiral2));
        float arms = arm1 * 0.7 + arm2 * 0.3;

        float turb = fbm(vec2(angle * 2.0 + time * rotation_speed * 0.3, dist * 8.0));
        float detail = fbm(vec2(angle * 5.0 - time * rotation_speed * 0.6, dist * 15.0 + time * 0.5));

        disk_color += inner_ring_color * arms * 0.5 * (1.0 - t);
        disk_color += vec3(0.3, 0.1, 0.5) * turb * 0.3;
        disk_color += inner_ring_color * detail * 0.15;

        float brightness = (1.0 - t) * 1.5 + 0.3;
        disk_color *= brightness;

        float alpha = smoothstep(event_horizon, r * 0.4, dist) * 0.9 + 0.1;
        col.rgb = mix(col.rgb, disk_color, alpha * (1.0 - t * 0.3));
        col.a = mix(col.a, 1.0, alpha);
    }
    else if (dist < r * 3.5) {
        float t = (dist - r) / (r * 2.5);
        float glow = exp(-3.0 * t) * distortion_strength;

        float photon_ring = exp(-20.0 * pow(t, 2.0)) * 1.5;
        vec3 ring_col = inner_ring_color * photon_ring;

        float outer_spiral = fract(angle * 2.0 / 6.28318 + time * rotation_speed * 0.2);
        float outer_arm = smoothstep(0.0, 0.2, outer_spiral) * (1.0 - smoothstep(0.7, 1.0, outer_spiral));
        vec3 outer_disk = mid_ring_color * outer_arm * glow * 0.4;

        float outer_turb = fbm(vec2(angle * 3.0 + time * 0.3, dist * 5.0));
        vec3 outer_noise_col = glow_color * outer_turb * glow * 0.2;

        col.rgb += glow_color * glow * 0.6;
        col.rgb += ring_col;
        col.rgb += outer_disk;
        col.rgb += outer_noise_col;
    }

    if (explosion_intensity > 0.01) {
        float shock_r = explosion_intensity * 0.35;
        float shock_w = 0.08 + explosion_intensity * 0.02;
        float shockwave = 1.0 - smoothstep(0.0, shock_w, abs(dist - shock_r));

        float inner_shock_r = shock_r * 0.6;
        float inner_shock = 1.0 - smoothstep(0.0, shock_w * 0.5, abs(dist - inner_shock_r));

        vec3 shock_color = mix(vec3(0.8, 0.5, 1.0), vec3(1.0, 0.9, 0.7), shockwave);
        col.rgb += shock_color * shockwave * explosion_intensity * 0.8;
        col.rgb += inner_ring_color * inner_shock * explosion_intensity * 0.5;

        float debris = fbm(uv * 25.0 + vec2(time * 3.0, -time * 2.0));
        col.rgb += vec3(1.0, 0.6, 0.3) * debris * explosion_intensity * 0.15;
    }

    float star_distortion = fbm(uv * 40.0 + vec2(time * 0.8, time * 0.3));
    float star_mask = smoothstep(r * 0.8, r * 2.0, dist) * (1.0 - smoothstep(r * 2.0, r * 4.0, dist));
    col.rgb += vec3(0.8, 0.7, 1.0) * star_distortion * 0.015 * star_mask * distortion_strength;

    COLOR = col;
}
