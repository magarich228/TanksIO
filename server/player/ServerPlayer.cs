using Godot;

// public partial class ServerPlayer : CharacterBody2D
// {
// 	/// <summary>
// 	/// Скорость движения вперед/назад
// 	/// </summary>
// 	[Export] 
// 	public float MoveSpeed = 200.0f;
// 	
// 	/// <summary>
// 	/// Скорость поворота в радианах/сек
// 	/// </summary>
// 	[Export] 
// 	public float RotationSpeed = 2.5f;
// 	
// 	public override void _PhysicsProcess(double delta)
// 	{
// 		float moveInput = Input.GetAxis("ui_down", "ui_up"); // -1 назад, +1 вперед
// 		float rotateInput = Input.GetAxis("ui_left", "ui_right"); // -1 влево, +1 вправо
//
// 		Rotation += rotateInput * RotationSpeed * (float)delta;
//
// 		Vector2 direction = new Vector2(0, -1).Rotated(Rotation);
// 		
// 		Velocity = direction * moveInput * MoveSpeed;
// 		
// 		MoveAndSlide();
// 	}
// }
