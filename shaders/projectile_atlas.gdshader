shader_type canvas_item;

varying vec4 atlas_rect;
varying vec4 vert_color;

void vertex() {
    atlas_rect = INSTANCE_CUSTOM;
    vert_color = COLOR;
}

void fragment() {
    vec2 atlas_uv = atlas_rect.xy + UV * atlas_rect.zw;
    vec4 tex_color = texture(TEXTURE, atlas_uv);

    float luminance = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
    vec3 tinted = mix(tex_color.rgb, vert_color.rgb, 1.0 - luminance);

    COLOR = vec4(tinted, tex_color.a * vert_color.a);
}