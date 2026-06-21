using Godot;
using System;
[GlobalClass]
public partial class Hurtbox : Area2D
{
	public delegate void HurtEventHandler(Hitbox hitbox);
	public event HurtEventHandler OnHurt;
	[Export] public HurtType HurtType { get; set; }
	public Hurtbox()
	{
		CollisionLayer = CollisionLayers.Hurtboxes;
        CollisionMask = CollisionLayers.Hitboxes;
		AreaEntered += OnAreaEntered;
	}

	protected virtual void OnAreaEntered(Area2D area)
	{
		if (area is Hitbox hitbox && hitbox.HitType == HurtType)
			OnHurt?.Invoke(hitbox);
	}
}
