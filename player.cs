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
	public int fastFallSpeed = 1500;
	[Export]
	public float fastFallDoubleTapWindowInSeconds = 0.5f;
	private float fastFallDoubleTapWindow = 0;
	private bool isFastFalling = false;
	public bool hasPressedFastFall = false;
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
	public int dashLength = 20;
	[Export]
	public bool dashBuildsUpSpeed = true;
	[Export]
	public int reachesFullDashSpeedWhenLengthIs = 15;
	[Export]
	public bool dashDecaysSpeed = true;
	[Export]
	public int speedStartsDecayingWhenLengthIs = 5;
	[Export]
	public float minimumSpeedFactorWhenDecayingOrAccelerating = 0.1F;
	public int remainingDashLength = 0;
	public Vector2 currentDashDirection;


	// Attacks and collisions
	private PlayerHurtbox playerHurtbox;
	private bool isAttacked = false;
	[Export]
	public int knockbackX = 500;
	[Export]
	public int knockbackY = -200;
	[Export]
	public int knockbackDuration = 10;
	public int remainingKnockbackDuration = 0;
	public Vector2 currentKnockbackDirection;
	[Export]
	public int maxGroundedAttacks = 3;
	private int currentGroundedAttacks = 0;
	[Export]
	public int attackCooldown = 10;
	private int remainingAttackCooldown = 0;
	[Export]
	public static int groundedAttackDamage = 1;
	[Export]
	public static int airAttackDamage = groundedAttackDamage;
	private Timer fastFallTimer;
	[Export]
	public int bounceDuration = 10;
	private int remainingBounceDuration = 0;
	[Export]
	public int bounceX = 200;
	[Export]
	public int bounceY = -400;
	private Vector2 bounceDirection;

	

	public override void _Ready()
	{
		playerHurtbox = GetNode<PlayerHurtbox>("PlayerHurtbox");
		playerHurtbox.AreaEntered += _on_PlayerHurtbox_area_entered;
	}


	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		bool isJumping = Input.IsActionJustPressed("ui_jump");
		bool isDashing = Input.IsActionJustPressed("ui_dash");
		bool isHoldingJump = Input.IsActionPressed("ui_jump");


		if (IsOnFloor() && velocity.Y == 0) {
			resetDash();
			resetFastFall();
		}

		// Movement and actions

		if (isAttacked) {
			velocity = GetKnockedBack(velocity);
			isAttacked = false;
		} else if (IsCurrentlyBouncing()) {
			remainingBounceDuration--;
			velocity = bounceDirection;
		} else if (IsCurrentlyKnockedBack()) {
			remainingKnockbackDuration--;
			velocity = currentKnockbackDirection;
		} else if (IsCurrentlyDashing()) {
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

		} else if (isFastFalling) {
			velocity.Y = fastFallSpeed;
		} else{
			if (Velocity.Y != 0) {
				currentJumpHoldingFrames = maximumJumpHoldingFrames;
			}

			// Déplacement normal

			// Gravité
			velocity.Y += (gravity * (float) delta) + 1;

			// Directions
			velocity = MoveHorizontally(velocity);
		}

		Velocity = velocity;
		MoveAndSlide();	
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_filedialog_refresh")) {
			GetTree().ReloadCurrentScene();
		}

		RegenDashPassively();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_down")) {
			if (hasPressedFastFall) {
				isFastFalling = true;

				// Create a Timer node
				Timer timer = new Timer
				{
					WaitTime = fastFallDoubleTapWindowInSeconds, // Set the duration here (e.g., 2 seconds)
					OneShot = true // Ensure the timer only runs once
				};

				// Add the timer to the scene tree
				AddChild(timer);

				// Connect the timeout signal to the resetFastFall method, passing the timer as an argument
				timer.Connect("timeout", new Callable(this, MethodName.OnFastFallTimerTimeout));

				fastFallTimer = timer;

				// Start the timer
				timer.Start();
			} else if (!isFastFalling) {
				fastFallDoubleTapWindow = fastFallDoubleTapWindowInSeconds;
				hasPressedFastFall = true;
			}
		}
	}

	private void OnFastFallTimerTimeout() {
		if (isFastFalling) {
			return;

		}
		resetFastFall(fastFallTimer);
	}

	private void resetFastFall(Timer timer = null) {
		fastFallDoubleTapWindow = 0;
		isFastFalling = false;
		hasPressedFastFall = false;

		timer?.QueueFree();
	}

	private void resetDash() {
		currentJumpHoldingFrames = 0;
		remainingDashLength = 0;
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
			return applyAccelerationOrDecay(currentDashDirection);
		}

		if (hasFullDash) {
			return applyAccelerationOrDecay(DoFullDash(velocity));
		}

		if (hasHorizontalDashOnly) {
			return applyAccelerationOrDecay(DoHorizontalDash(velocity));
		}

		if (hasVerticalDashOnly) {
			return applyAccelerationOrDecay(DoVerticalDash(velocity));
		}

		return velocity;
	}
	
	private Vector2 applyAccelerationOrDecay (Vector2 velocity) {
		float? factor = null;

		if (
			dashBuildsUpSpeed
			&& remainingDashLength >= reachesFullDashSpeedWhenLengthIs
		) {
			float accelerationFrames = dashLength - reachesFullDashSpeedWhenLengthIs;
			float elapsedAccelerationFrames = dashLength - remainingDashLength;

			factor = Math.Max(minimumSpeedFactorWhenDecayingOrAccelerating, (elapsedAccelerationFrames / accelerationFrames));
		} else if (
			dashDecaysSpeed
			&& remainingDashLength <= speedStartsDecayingWhenLengthIs
		) {
			factor = Math.Max(minimumSpeedFactorWhenDecayingOrAccelerating, ((float) remainingDashLength / (float) speedStartsDecayingWhenLengthIs));
		}

		if (null != factor) {
			velocity.X *= (float) factor;
			velocity.Y *= (float) factor;
		}

		return velocity;
	}

	private Vector2 DoFullDash(Vector2 velocity) {
		velocity = AddHorizontalDashPower(velocity);
		velocity = AddVerticalDashPower(velocity);

		currentDashDirection = velocity;

		return velocity;
	}

	private Vector2 DoHorizontalDash(Vector2 velocity) {
		velocity.Y = 0;
		velocity = AddHorizontalDashPower(velocity);

		currentDashDirection = velocity;

		return velocity;
	}

	private Vector2 DoVerticalDash(Vector2 velocity) {
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

	public void _on_PlayerHurtbox_area_entered(Area2D area)
	{
		if (!area.IsInGroup("enemyHurtbox") || IsCurrentlyBouncing()) {
			return;
		}

		if (IsCurrentlyDashing()) {
			GetBounced(Velocity);
		} else{
			isAttacked = true;
		}
	}

	public Vector2 GetBounced(Vector2 velocity)
	{
		resetDash();
		GD.Print("Bounced");

		remainingBounceDuration = bounceDuration;

		velocity.X = bounceX * -lastInputDirection;
		velocity.Y = bounceY;

		bounceDirection = velocity;

		return velocity;
	}

	public bool IsCurrentlyBouncing() {
		return 0 < remainingBounceDuration;
	}

	public Vector2 GetKnockedBack(Vector2 velocity)
	{
		remainingKnockbackDuration = knockbackDuration;

		velocity.X = knockbackX * -lastInputDirection;
		velocity.Y = knockbackY;

		currentKnockbackDirection = velocity;

		return velocity;
	}

	public bool IsCurrentlyDashing() {
		return 0 < remainingDashLength;
	}

	public bool IsCurrentlyKnockedBack() {
		return 0 < remainingKnockbackDuration;
	}
}
