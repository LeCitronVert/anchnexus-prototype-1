using Godot;
using System;

public partial class EnemyHealthBar : ProgressBar
{
	private Enemy enemy;

	public override void _Ready()
	{
		enemy = GetParent<Enemy>();

		enemy.UpdateHealthUi += UpdateHealthUiEventHandler;
		UpdateHealthUiEventHandler();
		
	}

	public void UpdateHealthUiEventHandler() {
		Value = enemy.hp;
	}
}
