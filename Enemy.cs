using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public int hp = 3;

	public override void _PhysicsProcess(double delta)
	{
		if (hp <= 0)
		{
			QueueFree();
		}
	}

	public void _on_Hurtbox_area_entered(Area2D area)
	{
		if (area.IsInGroup("player_attack"))
		{
			hp--;
		}
	}

	[Signal]
	public delegate void UpdateHealthUiEventHandler();
}
