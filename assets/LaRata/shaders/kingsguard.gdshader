shader_type canvas_item;
render_mode unshaded;
uniform vec4 accent : source_color = vec4(0.12, 0.85, 0.57, 1.0);
uniform float chest_energy = 0.0;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    vec4 source = texture(TEXTURE, UV);
    float hi = max(source.r, max(source.g, source.b));
    float lo = min(source.r, min(source.g, source.b));
    float luminance = dot(source.rgb, vec3(0.2126, 0.7152, 0.0722));
    // Dark neutral steel; saturated cloth/magic and bright highlights become emerald.
    float detail = max(smoothstep(0.13, 0.42, hi - lo), smoothstep(0.65, 0.94, luminance) * 0.6);
    vec3 steel = mix(vec3(0.008, 0.015, 0.018), vec3(0.25, 0.31, 0.30), luminance);
    vec3 emerald = accent.rgb * (0.18 + luminance * 0.95);
    vec3 armor = mix(steel, emerald, detail);
    // v0.107.1 native Flail Knight atlas: breastplate region. UVs stay attached
    // to the animated mesh, including turns, flails, hit reactions and death.
    vec2 plate = (UV - vec2(0.83715, 0.64054)) / vec2(0.08564, 0.23901);
    float inside = step(0.02, plate.x) * step(plate.x, 0.98) * step(0.08, plate.y) * step(plate.y, 0.94);
    float stem = abs(plate.x - (0.38 + 0.025 * sin(plate.y * 18.0)));
    float jag = 0.028 * sin(plate.x * 32.0) + 0.013 * sin(plate.x * 77.0);
    float left_branch = abs(plate.y - (0.40 + (0.38 - plate.x) * 0.65 + jag)) + max(0.0, plate.x - 0.38);
    float right_branch = abs(plate.y - (0.46 + (plate.x - 0.38) * 0.48 + jag)) + max(0.0, 0.38 - plate.x);
    float lower_branch = abs(plate.y - (0.72 - (plate.x - 0.38) * 0.35 + jag));
    float line = min(stem, min(left_branch, min(right_branch, lower_branch)));
    float vein = 1.0 - smoothstep(0.009, 0.027, line);
    float halo = 1.0 - smoothstep(0.02, 0.10, line);
    float pulse = 0.82 + 0.18 * sin(TIME * 1.1 - plate.y * 3.0);
    armor += chest_energy * inside * pulse * (accent.rgb * (vein * 0.78 + halo * 0.28) + vec3(0.30, 0.45, 0.28) * vein * 0.20);
    COLOR = vec4(armor, source.a) * tint;
}
