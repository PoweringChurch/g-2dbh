using System;
using Godot;

public partial class PlayerCharacter : Node2D
{
    public Vector2I ScreenResolution;
    public string textureName = "default";
    float baseSpeed = 75f;
    int iframes = 0;
    int framesFocusHeld = 0; // for fading hurtbox display
    int grazeFrames = 0; // for showing graze
    int health = 0;
    int scoreTimer = 15;
    // references
    Hurtbox hurtbox;
    Hurtbox grazebox;
    Sprite2D hurtboxDisplay;
    Sprite2D grazeDisplay;
    Sprite2D characterDisplay;
    LevelLoader loader;
    public void SetHealth(int to) => health = to;
    public override void _Ready()
    {
        hurtbox = GetNode<Hurtbox>("Hurtbox");
        grazebox = GetNode<Hurtbox>("Grazebox");
        hurtboxDisplay = GetNode<Sprite2D>("HurtboxDisplay");
        grazeDisplay = GetNode<Sprite2D>("GrazeDisplay");
        loader = GetNode<LevelLoader>("/root/LevelLoader");
        characterDisplay = GetNode<Sprite2D>("CharacterDisplay");
        characterDisplay.Texture = RenderingUtils.LoadTexture("user://data/characters/", textureName);
        characterDisplay.Texture ??= ResourceLoader.Load<Texture2D>("res://data/characters/default.png");
        hurtbox.OnHurt += _OnHurt;
        grazebox.OnHurt += _OnGraze;
    }
    private void Movement(float dt)
    {
        Vector2 inputDirection = Input.GetVector("left", "right", "up", "down").Normalized();
        bool focused = Input.IsActionPressed("focus");
        framesFocusHeld = Math.Clamp(framesFocusHeld + (focused ? 1 : -1), 0, 10);
        float speed =  focused ? baseSpeed*0.5f : baseSpeed;
        Position += inputDirection*speed*dt;
        Vector2 newPosition = Position + inputDirection*speed*dt;
        newPosition.X = Mathf.Clamp(newPosition.X, 0, ScreenResolution.X);
        newPosition.Y = Mathf.Clamp(newPosition.Y, 0, ScreenResolution.Y);
        Position = newPosition;
    }
    public override void _PhysicsProcess(double dt)
    {
        Movement((float)dt);
        hurtboxDisplay.Modulate = new Color(1,1,1,framesFocusHeld/10f);
        characterDisplay.Modulate = new Color (1,1,1,1-(framesFocusHeld/20f));
        grazeDisplay.Modulate = new Color (1,1,1,grazeFrames--/20f);
        if (iframes-- > 0) {
            float a = (iframes / 4 % 2 == 0) ? 0.5f : 0.75f;
            characterDisplay.Modulate = new Color(1, 1, 1, a);
        }
        if (--scoreTimer <= 0)
        {
            scoreTimer = 15;
            loader.IncrementScore(10*health);
        }
    }
    protected void _OnHurt(Hitbox hitbox)
    {
        if (iframes > 0) 
            return;
        iframes = 120;
        if (hitbox is Projectile proj && !proj.Persistant)
            proj.QueueFree();
        health--;
        loader.DecrementHealth(1);
        if (health <= 0)
            loader.FinishLevel();
    }
    protected void _OnGraze(Hitbox _) 
    {
        if (iframes > 0)
            return;
        grazeFrames = 20;
        loader.IncrementGraze(1);
    }
}
