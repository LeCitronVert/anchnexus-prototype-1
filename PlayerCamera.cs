using Godot;
using System;

public partial class PlayerCamera : Camera2D
{
	[Export]
	public float CameraSpeed = 2f;

	public override void _Ready()
	{
		PositionSmoothingSpeed = CameraSpeed;
		PositionSmoothingEnabled = true;
	}
}
