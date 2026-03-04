shader_type spatial;
render_mode blend_mix, depth_draw_always, cull_disabled, diffuse_burley, specular_schlick_ggx;

// Atlas texture (shared with terrain)
uniform sampler2D atlas_texture : source_color, filter_nearest, repeat_disable;
uniform vec2 tile_size = vec2(0.25, 0.25);
uniform vec2 atlas_texel_size = vec2(0.015625, 0.015625);

// Water appearance
uniform float water_alpha : hint_range(0.0, 1.0) = 0.55;
uniform float fresnel_power : hint_range(0.5, 5.0) = 2.5;

// UV scroll animation
uniform float uv_scroll_speed : hint_range(0.0, 0.1) = 0.02;

// Distance fog (should match terrain shader values)
uniform vec4 fog_color : source_color = vec4(0.65, 0.75, 0.85, 1.0);
uniform float fog_start = 60.0;
uniform float fog_end = 128.0;
uniform float fog_height_falloff : hint_range(0.0, 0.1) = 0.01;

void vertex() {
    // Lower top face slightly below block edge for a natural water level look
    if (NORMAL.y > 0.5) {
        VERTEX.y -= 0.12;
    }
}

void fragment() {
    // --- Atlas UV with tiling ---
    vec2 tile_uv = fract(UV);

    // Scroll UVs on top faces for a flowing effect
    if (NORMAL.y > 0.5) {
        tile_uv += vec2(TIME * uv_scroll_speed, TIME * uv_scroll_speed * 0.6);
        tile_uv = fract(tile_uv);
    }

    // Half-texel inset to prevent atlas bleeding
    vec2 half_texel = atlas_texel_size * 0.5;
    vec2 inset = half_texel / tile_size;
    vec2 safe_uv = clamp(tile_uv, inset, vec2(1.0) - inset);

    vec2 atlas_uv = UV2 + safe_uv * tile_size;
    vec4 tex_color = texture(atlas_texture, atlas_uv);

    // Apply vertex color tint (RGB = block tint from data)
    vec3 base_color = tex_color.rgb * COLOR.rgb;

    // --- Fresnel: more opaque at grazing angles ---
    float fresnel = pow(1.0 - abs(dot(NORMAL, VIEW)), fresnel_power);
    fresnel = clamp(fresnel, 0.0, 1.0);

    ALBEDO = base_color;
    ALPHA = mix(water_alpha, min(water_alpha + 0.35, 1.0), fresnel);
    ROUGHNESS = 0.05;
    METALLIC = 0.0;
    SPECULAR = 0.5;

    // --- Distance fog ---
    float dist = length(VERTEX);
    vec3 world_pos_fog = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float height_factor = exp(-max(world_pos_fog.y, 0.0) * fog_height_falloff);
    float fog_factor = smoothstep(fog_start, fog_end, dist) * height_factor;
    ALBEDO = mix(ALBEDO, fog_color.rgb, fog_factor);
    ALPHA = mix(ALPHA, 1.0, fog_factor);
}
