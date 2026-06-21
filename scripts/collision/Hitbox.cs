using Godot;
using System;

public enum HurtType{ Enemy, Friendly, None}
public static class CollisionLayers
{
    public const uint Hurtboxes          = 1 << 0; // layer 1
    public const uint Hitboxes         = 1 << 1; // layer 2
}
[GlobalClass]
public partial class Hitbox : Area2D
{
	[Export] public HurtType HitType { get; set; }
	public Hitbox()
	{
		CollisionLayer = CollisionLayers.Hitboxes;
		CollisionMask = CollisionLayers.Hurtboxes;
	}
}
