using System;
using Godot;

public partial class PlayerCharacter : Node2D
{
    public const float HurtRadius = 3;
    public const float GrazeRadius = 10;
    public const float BaseSpeed = 75f;
    [Export] Sprite2D CharacterDisplay;
    [Export] Sprite2D hurtboxDisplay;
    [Export] Sprite2D grazeDisplay;
    public Vector2I ScreenResolution;
    int iframes = 0;
    int framesFocusHeld = 0; // for fading hurtbox display
    int grazeFrames = 0; // for showing graze

    [Signal] public delegate void OnHurtEventHandler();
    [Signal] public delegate void OnGrazeEventHandler();
    public void ApplyTextureOfName(string name)
    {
        CharacterDisplay.Texture = RenderingUtils.LoadTexture("res://data/characters/"+name);
    }
    public void Movement(double dt)
    {
        Vector2 inputDirection = Input.GetVector("left", "right", "up", "down").Normalized();
        bool focused = Input.IsActionPressed("focus");
        framesFocusHeld = Math.Clamp(framesFocusHeld + (focused ? 1 : -1), 0, 10);
        float speed =  (focused ? BaseSpeed*0.5f : BaseSpeed) * (ConfigHelper.Current.SlowMovement ? 0.5f : 1);
        Position += inputDirection*speed*(float)dt;
        Vector2 newPosition = Position + inputDirection*speed*(float)dt;
        newPosition.X = Mathf.Clamp(newPosition.X, 0, ScreenResolution.X);
        newPosition.Y = Mathf.Clamp(newPosition.Y, 0, ScreenResolution.Y);
        Position = newPosition;
    }
    public void VisualFeedback()
    {
        hurtboxDisplay.Modulate = new Color(1,1,1,framesFocusHeld/10f);
        CharacterDisplay.Modulate = new Color (1,1,1,1-(framesFocusHeld/20f));
        grazeDisplay.Modulate = new Color (1,1,1,grazeFrames--/20f);
        if (iframes-- > 0) {
            float a = (iframes / 4 % 2 == 0) ? 0.5f : 0.75f;
            CharacterDisplay.Modulate = new Color(1, 1, 1, a);
        }
    }
    public bool Hurt()
    {
        if (iframes > 0)
            return false;
        iframes = 120;
        EmitSignal(SignalName.OnHurt);
        return true;
    }
    public bool Graze() 
    {
        if (iframes > 0)
            return false;
        grazeFrames = 20;
        EmitSignal(SignalName.OnGraze);
        return true;
    }
}
