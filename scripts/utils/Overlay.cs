using Godot;
using System;
using System.Collections;

public partial class Overlay : CanvasLayer
{
    public static Overlay Inst;
    public static bool ShowHitboxes { get; private set; }
    [Export] Label FPS;
    [Export] Label FrameTime;
    [Export] Label ActiveProjectiles;
    [Export] Label Queued;
    [Export] Label Time;
    [Export] Label DisplayHitboxes;
    [Export] Label Culled;
    GameSession gs;
    public override void _Ready()
    {
        Inst = this;
        gs = GetNode<GameSession>("/root/GameSession");
    }
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_overlay"))
            Visible = !Visible;
        if (@event.IsActionPressed("show_hitboxes"))
            ShowHitboxes = !ShowHitboxes;    
    }
    public override void _Process(double dt)
    {
        SyncInfo(dt);
    }

    public void SyncInfo(double dt = -1, int queuedCount = -1, int activeProjectiles = -1, int culled = -1)
    {
        if (dt != -1)
        {
            FrameTime.Text = (dt*1000).ToString("F2");
            FPS.Text = (1/dt).ToString("F2");
        }
        if (queuedCount != -1)
        {
            Queued.Text = queuedCount.ToString();
            ActiveProjectiles.Text = $"{activeProjectiles} / {LevelDirector.MaxBulletCount}";
            Time.Text = gs.Elapsed.ToString("F2");
            DisplayHitboxes.Text = ShowHitboxes ? "On" : "Off";
        }
        if (culled != -1)
        {
            Culled.Text = culled.ToString();
        }
    }
}
