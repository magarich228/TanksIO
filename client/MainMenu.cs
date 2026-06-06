using Godot;
using SysEnvironment = System.Environment;

public partial class MainMenu : Control
{
	public void OnPlayButtonDown()
	{
		GD.Print("play btn down");
	}
	
	private void OnExitButtonDown()
	{
		SysEnvironment.Exit(0);
	}
}
