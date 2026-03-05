shader_type spatial;

render_mode blend_mix, depth_draw_never, cull_back, specular_disabled, unshaded;

uniform float crack_progress : hint_range(0.0, 1.0) = 0.0;

// Procedural crack pattern using layered noise
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float crack_pattern(vec2 uv, float progress) {
    float n1 = noise(uv * 4.0);
    float n2 = noise(uv * 8.0 + vec2(5.2, 1.3));
    float n3 = noise(uv * 16.0 + vec2(9.7, 3.1));

    float combined = n1 * 0.5 + n2 * 0.3 + n3 * 0.2;

    // Create crack lines by thresholding noise — more lines as progress grows
    float threshold = 1.0 - progress;
    float crack = smoothstep(threshold, threshold + 0.1, combined);
    return crack;
}

void fragment() {
    if (crack_progress <= 0.0) {
        discard;
    }

    vec2 uv = UV;
    float crack = crack_pattern(uv, crack_progress);

    if (crack <= 0.01) {
        discard;
    }

    // Dark crack lines with increasing opacity as mining progresses
    ALBEDO = vec3(0.0, 0.0, 0.0);
    ALPHA = crack * (0.4 + crack_progress * 0.5);
}
