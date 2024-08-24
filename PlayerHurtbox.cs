using Godot;
using System;

public partial class PlayerHurtbox : Area2D
{
	private player player;

	public override void _Ready()
	{
		player = GetParent<player>();
	}
}
