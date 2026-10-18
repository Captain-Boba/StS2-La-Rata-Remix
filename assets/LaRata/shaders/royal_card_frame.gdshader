shader_type canvas_item;
render_mode unshaded;

// Recolor the native sculpted frame: readable charcoal, jade metal and emerald
// engraving highlights. Keep its texture, silhouette and alpha; no UI overlays.
varying vec4 vertex_tint;
void vertex() {
    vertex_tint = COLOR;
}
void fragment() {
    vec4 source = texture(TEXTURE, UV);
    float relief = dot(source.rgb, vec3(0.299, 0.587, 0.114));
    vec3 charcoal = mix(vec3(0.09, 0.12, 0.115), vec3(0.24, 0.31, 0.28), relief);
    vec3 jade = mix(charcoal, vec3(0.20, 0.53, 0.38), smoothstep(0.48, 0.78, relief));
    vec3 enamel = mix(jade, vec3(0.60, 0.82, 0.62), smoothstep(0.80, 1.0, relief));
    COLOR = vec4(enamel, source.a) * vertex_tint;
}
