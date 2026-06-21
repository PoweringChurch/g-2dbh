using System;
using Godot;

public partial class PlayerCharacter : Node2D
{
    public string characterSprite = "default";
    float baseSpeed = 100f;
    int iframes = 0;
    int framesFocusHeld = 0; // for fading hurtbox display
    int grazeFrames = 0; // for showing graze
    int health = 0;
    int grazeCount = 0;
    int score = 0;
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

        if (!FileAccess.FileExists("res://data/characters/"+characterSprite+".png"))
            {GD.Print($"[PlayerCharacter] Character image of name '{characterSprite}' is invalid, using default sprite"); characterSprite = "default";}
        characterDisplay.Texture = ResourceLoader.Load<Texture2D>("res://data/characters/"+characterSprite+".png");
        hurtbox.OnHurt += _OnHurt;
        grazebox.OnHurt += _OnGraze;
        loader.LevelFinished += _OnLevelFinished;
    }
    private void Movement(float dt)
    {
        Vector2 inputDirection = Input.GetVector("left", "right", "up", "down").Normalized();
        bool focused = Input.IsActionPressed("focus");

        framesFocusHeld = Math.Clamp(framesFocusHeld + (focused ? 1 : -1), 0, 10);
        hurtboxDisplay.Modulate = new Color(1,1,1,framesFocusHeld/10f);
        characterDisplay.Modulate = new Color (1,1,1,1-(framesFocusHeld/20f));
        grazeDisplay.Modulate = new Color (1,1,1,grazeFrames--/20f);

        float speed =  focused ? baseSpeed*0.5f : baseSpeed;
        Position += inputDirection*speed*dt;
    }
    public override void _PhysicsProcess(double dt)
    {
        Movement((float)dt);
        // after movement to ensure player transparency is correct
        if (iframes > 0) {
            iframes--;
            float a = (iframes / 4 % 2 == 0) ? 0.5f : 0.75f;
            characterDisplay.Modulate = new Color(1, 1, 1, a);
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
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.HUD.SetHealth(health);
        if (health <= 0)
        {
            ui.ShowGameOver();
            loader.Abort();
        }
    }
    protected void _OnGraze(Hitbox _) 
    {
        var ui = GetNode<UIManager>("/root/UIManager");
        grazeFrames = 20;
        grazeCount++;
        score += 10*health;
        ui.HUD.SetGraze(grazeCount);
    }
    protected void _OnLevelFinished(string levelName)
    {
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.ScoreSummary.SetGraze(grazeCount);
        ui.ScoreSummary.SetHP(health);
        ui.ScoreSummary.SetScore(score);
    }
}
