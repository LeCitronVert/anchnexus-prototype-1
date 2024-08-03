using Godot;
using System;
using System.Text.RegularExpressions;

public partial class DashBar : ProgressBar
{
	private player player;

	public override void _Ready()
	{
		player = GetNode<player>("%Player");

		player.UpdateDashUi += UpdateDashUiEventHandler;
		UpdateDashUiEventHandler();
		
	}

	public void UpdateDashUiEventHandler() {
		Value = player.currentDash;
		SetProgressBarColor();
	}

	private void SetProgressBarColor() {
		string color;
		StyleBoxFlat progressBarStyle = new StyleBoxFlat();

		if ((double) player.maxDash == player.currentDash) {
			color = "#42f545";
		} else if ((double) (player.maxDash / 2) <= player.currentDash) {
			color = "#f5c542";
		} else {
			color = "#f54242";
		}
		

		progressBarStyle.BgColor = new Color(color);

		AddThemeStyleboxOverride("fill", progressBarStyle);
	}
}
