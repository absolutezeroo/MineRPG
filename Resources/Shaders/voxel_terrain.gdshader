shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

// Atlas texture containing all block tile textures
uniform sampler2D atlas_texture : source_color, filter_nearest, repeat_disable;
// Size of one tile in normalized atlas space (1/columns, 1/rows)
uniform vec2 tile_size = vec2(0.25, 0.25);
// Size of one texel in atlas space (1/atlas_pixel_width, 1/atlas_pixel_height)
uniform vec2 atlas_texel_size = vec2(0.015625, 0.015625);

// Vertex AO strength (0 = no vertex AO, 1 = full vertex AO darkening)
uniform float vertex_ao_strength : hint_range(0.0, 1.0) = 0.6;

// Edge AO intensity: how dark the edges get (0 = off, 1 = fully dark)
uniform float edge_ao_intensity : hint_range(0.0, 1.0) = 0.2;
// Edge AO spread: how wide the darkening effect is at block edges
uniform float edge_ao_spread : hint_range(0.01, 0.5) = 0.12;

// Distance fog
uniform vec4 fog_color : source_color = vec4(0.65, 0.75, 0.85, 1.0);
uniform float fog_start = 60.0;
uniform float fog_end = 128.0;
uniform float fog_height_falloff : hint_range(0.0, 0.1) = 0.01;

// Subtle top-face brightness boost (simulates sky ambient on upward faces)
uniform float top_light_boost : hint_range(0.0, 0.5) = 0.12;

void fragment() {
    // --- Atlas UV with tiling ---
    // UV  = tiling coordinates (0..width, 0..height) from greedy meshing
    // UV2 = atlas tile origin (u0, v0)
    vec2 tile_uv = fract(UV);

    // Half-texel inset to prevent atlas bleeding at tile boundaries.
    vec2 half_texel = atlas_texel_size * 0.5;
    vec2 inset = half_texel / tile_size;
    vec2 safe_uv = clamp(tile_uv, inset, vec2(1.0) - inset);

    vec2 atlas_uv = UV2 + safe_uv * tile_size;
    vec4 tex_color = texture(atlas_texture, atlas_uv);

    // Vertex color: RGB = block tint, A = vertex AO (0=occluded, 1=fully lit)
    vec3 base_color = tex_color.rgb * COLOR.rgb;

    // --- Vertex ambient occlusion ---
    // COLOR.a carries per-vertex AO computed by the mesher (0=dark, 1=bright).
    float vertex_ao = mix(1.0, COLOR.a, vertex_ao_strength);

    // --- Edge ambient occlusion ---
    // Subtle darkening at block edges for extra depth.
    vec2 edge_dist = abs(tile_uv - 0.5) * 2.0;
    float edge_factor = max(edge_dist.x, edge_dist.y);
    float edge_ao = 1.0 - smoothstep(1.0 - edge_ao_spread * 2.0, 1.0, edge_factor) * edge_ao_intensity;

    // --- Directional ambient ---
    float top_boost = 1.0 + max(NORMAL.y, 0.0) * top_light_boost;
    float bottom_dim = 1.0 + min(NORMAL.y, 0.0) * 0.15;

    ALBEDO = base_color * vertex_ao * edge_ao * top_boost * bottom_dim;

    // Fully opaque — do NOT write ALPHA (would cause transparent-pass rendering
    // with broken depth sorting). Leaves use discard via alpha test instead.
    if (tex_color.a < 0.5) {
        discard;
    }

    ROUGHNESS = 0.85;
    METALLIC = 0.0;

    // --- Distance fog ---
    float dist = length(VERTEX);
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float height_factor = exp(-max(world_pos.y, 0.0) * fog_height_falloff);
    float fog_factor = smoothstep(fog_start, fog_end, dist) * height_factor;
    ALBEDO = mix(ALBEDO, fog_color.rgb, fog_factor);
}
