using Godot;
using System;

public partial class EnemyHurtbox : Area2D
{
	public override void _Ready()
	{
		AddToGroup("enemyHurtbox");
	}
}
