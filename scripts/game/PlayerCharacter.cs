using System;
using Godot;

public partial class PlayerCharacter : Node2D
{
    public const float HurtRadius = 5;
    public const float GrazeRadius = 10;
    public const float BaseSpeed = 75f;
    public string TextureName = "default";
    public Vector2I ScreenResolution;
    int iframes = 0;
    int framesFocusHeld = 0; // for fading hurtbox display
    int grazeFrames = 0; // for showing graze
    Sprite2D hurtboxDisplay;
    Sprite2D grazeDisplay;
    Sprite2D characterDisplay;
    [Signal] public delegate void OnHurtEventHandler();
    [Signal] public delegate void OnGrazeEventHandler();
    public override void _Ready()
    {
        hurtboxDisplay = GetNode<Sprite2D>("HurtboxDisplay");
        grazeDisplay = GetNode<Sprite2D>("GrazeDisplay");
        characterDisplay = GetNode<Sprite2D>("CharacterDisplay");
        characterDisplay.Scale = Vector2.One*ConfigHelper.Current.CharacterScale;
        characterDisplay.Texture = RenderingUtils.LoadTexture("user://data/characters/", ConfigHelper.Current.Character);
        characterDisplay.Texture ??= ResourceLoader.Load<Texture2D>("res://data/characters/default.png");
    }
    public void Movement(double dt)
    {
        Vector2 inputDirection = Input.GetVector("left", "right", "up", "down").Normalized();
        bool focused = Input.IsActionPressed("focus");
        framesFocusHeld = Math.Clamp(framesFocusHeld + (focused ? 1 : -1), 0, 10);
        float speed =  focused ? BaseSpeed*0.5f : BaseSpeed;
        Position += inputDirection*speed*(float)dt;
        Vector2 newPosition = Position + inputDirection*speed*(float)dt;
        newPosition.X = Mathf.Clamp(newPosition.X, 0, ScreenResolution.X);
        newPosition.Y = Mathf.Clamp(newPosition.Y, 0, ScreenResolution.Y);
        Position = newPosition;
    }
    public void VisualFeedback()
    {
        hurtboxDisplay.Modulate = new Color(1,1,1,framesFocusHeld/10f);
        characterDisplay.Modulate = new Color (1,1,1,1-(framesFocusHeld/20f));
        grazeDisplay.Modulate = new Color (1,1,1,grazeFrames--/20f);
        if (iframes-- > 0) {
            float a = (iframes / 4 % 2 == 0) ? 0.5f : 0.75f;
            characterDisplay.Modulate = new Color(1, 1, 1, a);
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
