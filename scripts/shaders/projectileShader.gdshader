shader_type canvas_item;

// Define how many columns and rows are in your sprite sheet
uniform int columns = 8;
uniform int rows = 8;

void vertex() {
    // INSTANCE_CUSTOM.r grabs the "Red" channel of the Color we pass in GDScript
    float frame = INSTANCE_CUSTOM.r;
    
    // Calculate which column and row this frame index belongs to
    float col = mod(frame, float(columns));
    float row = floor(frame / float(columns));
    
    // Shrink the UV mapping down to a single frame and offset it to the correct grid cell
    UV.x = (UV.x + col) / float(columns);
    UV.y = (UV.y + row) / float(rows);
}