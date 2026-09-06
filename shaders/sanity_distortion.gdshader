shader_type canvas_item;

uniform float distortion_strength : hint_range(0.0, 1.0) = 0.0;
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

// Pseudo-random noise, used for grain and glitch timing
float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    vec2 uv = SCREEN_UV;
    vec2 centered_uv = uv - 0.5;

    // --- Breathing pulse: slow, irregular "heartbeat" rather than a clean sine ---
    float pulse = sin(TIME * 1.3) * 0.5 + sin(TIME * 2.7) * 0.3;
    float breathing = 0.5 + 0.5 * pulse; // 0..1, irregular rhythm

    // --- Occasional violent glitch spikes (sparse, unpredictable) ---
    float glitch_time = floor(TIME * 6.0);
    float glitch_trigger = step(0.94, rand(vec2(glitch_time, 0.0))); // fires ~6% of the time
    float glitch_amount = glitch_trigger * distortion_strength;

    // Horizontal tearing during a glitch spike
    float tear = (rand(vec2(floor(uv.y * 40.0), glitch_time)) - 0.5) * 0.05 * glitch_amount;
    uv.x += tear;

    // --- Chromatic aberration: RGB channel split, scales with distortion + glitch ---
    float aberration = (0.004 * distortion_strength) + (0.02 * glitch_amount);
    vec4 col;
    col.r = textureLod(screen_texture, uv + vec2(aberration, 0.0), 0.0).r;
    col.g = textureLod(screen_texture, uv, 0.0).g;
    col.b = textureLod(screen_texture, uv - vec2(aberration, 0.0), 0.0).b;
    col.a = 1.0;

    // --- Heavy vignette: tunnel vision closing in as sanity drops, pulsing subtly ---
    float dist = length(centered_uv);
    float vignette_radius = 0.75 - (0.45 * distortion_strength) - (0.05 * breathing * distortion_strength);
    float vignette = smoothstep(vignette_radius, vignette_radius - 0.35, dist);
    // vignette = 1.0 in center (untouched), fades to 0.0 (black) at edges

    vec3 darkened = col.rgb * mix(1.0, vignette, distortion_strength);

    // --- Slight desaturation + sickly color tint ---
    float gray = dot(darkened, vec3(0.299, 0.587, 0.114));
    vec3 desaturated = mix(darkened, vec3(gray), distortion_strength * 0.5);
    vec3 tinted = mix(desaturated, vec3(0.05, 0.02, 0.03), distortion_strength * 0.25);

    // --- Film grain: constant subtle noise, more visible as sanity drops ---
    float grain = (rand(uv * TIME * 60.0) - 0.5) * 0.08 * distortion_strength;
    vec3 final_color = tinted + grain;

    COLOR = vec4(final_color, 1.0);
}
