using Godot;
using System;

public partial class player : CharacterBody2D
{
	// Movement
	[Export]
	public static int speed = 375;
	[Export]
	public static int gravity = 2500;
	[Export]
	public static int fastFallSpeed = 500;


	// Dash stuff
	[Export]
	public static int maxDash = 100;
	[Export]
	public double currentDash = maxDash;
	[Export]
	public int dashRegenPerSecond = maxDash / 2;
	[Export]
	public int dashRegenPerKill = maxDash / 2;
	[Export]
	public bool hasFullDash = true;
	[Export]
	public bool hasHorizontalDashOnly = false;
	[Export]
	public bool hasVerticalDashOnly = false;


	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		velocity.Y += (gravity * (float) delta) + (1 == Input.GetActionStrength("ui_down") ? fastFallSpeed : 0);

		// Get the input direction.
		float direction = Input.GetAxis("ui_left", "ui_right");
		velocity.X = direction * speed;

		Velocity = velocity;
		
		MoveAndSlide();
	}

	public override void _Process(double delta)
	{
		RegenDashPassively();
	}

	public void RegenDashPassively () {
		if (maxDash == currentDash) {
			return;
		}

		currentDash = Math.Min(
			maxDash,
			currentDash + (dashRegenPerSecond * 0.016)
		);


		EmitSignal(SignalName.UpdateDashUi);
	}

	public void RegenDashOnKill () {
		if (maxDash == currentDash) {
			return;
		}

		currentDash = Math.Min(
			maxDash,
			currentDash + dashRegenPerKill
		);

		EmitSignal(SignalName.UpdateDashUi);
	}


	[Signal]
	public delegate void UpdateDashUiEventHandler();
}
