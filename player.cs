using Godot;
using System;

public partial class player : CharacterBody2D
{
	// Movement
	[Export]
	public int speed = 375;
	[Export]
	public int gravity = 2500;
	[Export]
	public int fastFallSpeed = 500;
	[Export]
	public int jumpStrength = 200;
	[Export]
	public int heldJumpStrength = 100;
	[Export]
	public int maximumJumpHoldingFrames = 5;
	public int currentJumpHoldingFrames = 0;
	public float lastInputDirection = 1;



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
	[Export]
	public bool groundedDashOnly = false;
	[Export]
	public int dashStrength = 900;
	[Export]
	public int dashLength = 7;
	public int remainingDashLength = 0;
	public Vector2 currentDashDirection;


	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		bool isJumping = Input.IsActionJustPressed("ui_jump");
		bool isDashing = Input.IsActionJustPressed("ui_dash");
		bool isHoldingJump = Input.IsActionPressed("ui_jump");


		if (IsOnFloor() && velocity.Y == 0) {
			currentJumpHoldingFrames = 0;
			remainingDashLength = 0;
		}

		if (0 < remainingDashLength) {
			remainingDashLength--;
			velocity = DoDash(velocity, true);
		} else if (isJumping || isDashing) {
			velocity.Y = 0;

			// Jump
			if (isJumping && IsOnFloor() && velocity.Y == 0) {
				velocity = DoJump(velocity);
			}


			// Dash
			if (
				isDashing
				&& maxDash == currentDash 
				&& (!groundedDashOnly || IsOnFloor())
			) {
				velocity = DoDash(velocity);
			}

		} else if (
			isHoldingJump 
			&& !isJumping
			&& maximumJumpHoldingFrames > currentJumpHoldingFrames
			&& Velocity.Y != 0
		) {
			// Maintien du saut
			velocity = KeepJumping(velocity);

			// Directions
			velocity = MoveHorizontally(velocity);

		} else {
			if (Velocity.Y != 0) {
				currentJumpHoldingFrames = maximumJumpHoldingFrames;
			}

			// Déplacement normal

			// Gravité
			velocity.Y += (gravity * (float) delta) + (1 == Input.GetActionStrength("ui_down") ? fastFallSpeed : 1);

			// Directions
			velocity = MoveHorizontally(velocity);
		}

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

	public Vector2 DoDash(Vector2 velocity, bool isHoldingDash = false) {
		// todo : Ajouter un knockback en cas de collision avec un ennemi après le dash, sans dégâts subis
		ConsumeDash();
		if (!isHoldingDash) {
			remainingDashLength = dashLength;
		} else {
			return currentDashDirection;
		}

		if (hasFullDash) {
			return DoFullDash(velocity);
		}

		if (hasHorizontalDashOnly) {
			return DoHorizontalDash(velocity);
		}

		if (hasVerticalDashOnly) {
			return DoVerticalDash(velocity);
		}

		return velocity;
	}

	private Vector2 DoFullDash(Vector2 velocity) {
		GD.Print("dash : complet");	

		velocity = AddHorizontalDashPower(velocity);
		velocity = AddVerticalDashPower(velocity);

		currentDashDirection = velocity;

		return velocity;
	}

	private Vector2 DoHorizontalDash(Vector2 velocity) {
		GD.Print("dash : horizontal");	

		velocity.Y = 0;
		velocity = AddHorizontalDashPower(velocity);

		currentDashDirection = velocity;

		return velocity;
	}

	private Vector2 DoVerticalDash(Vector2 velocity) {
		GD.Print("dash : vertical");	

		velocity.X = 0;
		velocity = AddVerticalDashPower(velocity);

		currentDashDirection = velocity;

		return velocity;
	}

	private Vector2 AddHorizontalDashPower(Vector2 velocity) {
		float direction = Input.GetAxis("ui_left", "ui_right");

		// Direction par défaut (basée sur la dernière direction, puis sinon vers la droite)
		if (0 == direction) {
			direction = lastInputDirection;

			if (0 == direction) {
				direction = 1;
			}
		}

		velocity.X = direction * dashStrength;

		return velocity;
	}

	private Vector2 AddVerticalDashPower(Vector2 velocity) {
		float direction = Input.GetAxis("ui_up", "ui_down");

		// Direction par défaut (vers le haut uniquement si en dash vertical seulement)
		if (0 == direction && hasVerticalDashOnly) {
			direction = -1;
		}

		velocity.Y = direction * dashStrength;

		return velocity;
	}

	private Vector2 DoJump(Vector2 jump) {
		currentJumpHoldingFrames = 0;
		jump.Y -= jumpStrength;

		return jump;
	}

	private Vector2 KeepJumping(Vector2 jump) {
		currentJumpHoldingFrames++;
		jump.Y -= heldJumpStrength;

		return jump;
	}

	private Vector2 MoveHorizontally(Vector2 velocity) {
		// Directions
		float direction = Input.GetAxis("ui_left", "ui_right");
		velocity.X = direction * speed;

		if (0 != direction) {
			lastInputDirection = direction;
		}

		return velocity;
	}

	private void ConsumeDash() {
		currentDash = 0;
		EmitSignal(SignalName.UpdateDashUi);
	}
}
