using Godot;
using System;

public partial class PlayerAttack : CollisionShape2D
{
	public override void _Ready()
	{
		Disable();
	}

	public void Enable()
	{
		SetProcess(true);
		Show();
	}

	public void Disable()
	{
		SetProcess(false);
		Hide();
	}
}
