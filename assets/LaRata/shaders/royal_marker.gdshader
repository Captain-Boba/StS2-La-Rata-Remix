shader_type canvas_item;
render_mode unshaded;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    vec4 source = texture(TEXTURE, UV);
    float light = dot(source.rgb, vec3(0.2126, 0.7152, 0.0722));
    // Bright emerald silhouette remains readable even where the native art is dark.
    vec3 ink = mix(vec3(0.08, 0.62, 0.30), vec3(0.72, 1.0, 0.80), smoothstep(0.08, 0.78, light));
    COLOR = vec4(ink, source.a) * tint;
}
