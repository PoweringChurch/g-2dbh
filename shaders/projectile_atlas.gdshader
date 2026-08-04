shader_type canvas_item;
varying vec4 atlas_rect;
void vertex() {
    atlas_rect = INSTANCE_CUSTOM;
}
void fragment() {
    vec2 atlas_uv = atlas_rect.xy + UV * atlas_rect.zw;
    COLOR = texture(TEXTURE, atlas_uv); // i pretty much just removed the color mult
}