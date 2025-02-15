using System;
using System.Collections.Generic;
using Godot;
using Lib;

namespace JeuClient.Scripts.PlayerScripts;

public abstract partial class ClassScript : PlayerScript
{
	protected float GravityValue = 9.8f;
	
	//Variable des objets
	public bool PlayerIsHere = false;
	protected Node3D CameraH;
	protected SpringArm3D CameraV;
	public Camera3D Camera;
	protected Node3D CameraPlayer;
	protected Node3D PlayerMesh;
	protected AnimationPlayer AnimationPlayer;
	protected AnimationTree AnimationTree;
	protected Timer DamageTimer;
	protected int AnimationState = -1;
	protected bool IsAttacking;
	
	//Variable de camera
	private float _fovMax = 80;
	private float _fovMin = 40;
	
	//Variable de mouvement
	protected (int, int) DirectionControl = (0, 0);

	protected bool IsWalking;
	protected bool IsRunning;
	
	protected Vector3 Direction;
	protected Vector3 HorizontalVelocity;
	protected Vector3 Movement;
	protected Vector3 VerticalVelocity;
	protected float MovementSpeed;
	protected float AngularAcceleration = 10;
	protected int Acceleration = 15;

	public Control _inventory = GD.Load<PackedScene>("res://Scenes/HUD/Inventory.tscn").Instantiate<Control>();
	
	protected int _uiTimer;
	
	public bool Invincibility;
	public bool Agro;
	public bool Absorption;
	
	public SpringArm3D GetCameraVect()
	{
		return CameraV;
	}
	
	public Node3D GetCameraH()
	{
		return CameraH;
	}
	
	public Node3D GetCamera()
	{
		return CameraPlayer;
	}
	
	protected void InitPlayer()
	{
		Id = Conversions.AtoI(GameManager.InfoJoueur["id"]);
		Pseudo = GameManager.InfoJoueur["pseudo"];
		Classe = GameManager.InfoJoueur["class"];
		
		CameraPlayer = GetNode<Node3D>("CameraPlayer");
		Camera = GetNode<Camera3D>("CameraPlayer/h/v/Camera3D");
		CameraV = GetNode<SpringArm3D>("CameraPlayer/h/v");
		CameraH = GetNode<Node3D>("CameraPlayer/h");
		Direction = Vector3.Back.Rotated(Vector3.Up, CameraH.GlobalTransform.Basis.GetEuler().Y);
		
		PlayerMesh = GetNode<Node3D>("Player");
		
		DamageTimer = GetNode<Timer>("DamageTimer");
		AnimationTree = GetNode<AnimationTree>("AnimationTree");
		AnimationTree.Active = true;
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	protected void Zoom(InputEvent @event)
	{
		if (@event.AsText() == "Mouse Wheel Down")
		{
			if (Camera.Fov <= _fovMax)
			{
				Camera.Fov  += 2;
			}
		}
		else if (@event.AsText() == "Mouse Wheel Up")
		{
			if (Camera.Fov >= _fovMin)
			{
				Camera.Fov -= 2;
			}
		}
	}
	
	protected void SendPosition()
	{
		GameManager.InfoJoueur["co"] = $"{Position.X};{Position.Y};{Position.Z}";
		GameManager.InfoJoueur["orientation"] = $"{PlayerMesh.Rotation.X};{PlayerMesh.Rotation.Y};{PlayerMesh.Rotation.Z}";
		GameManager.InfoJoueur["hp"] = $"{GameManager.Health}";
		GameManager.InfoJoueur["mp"] = $"{GameManager.Stamina}";
		PlayerIsHere = true;
	}

	protected void HeathPlayer()
	{
		for (int i = 0; i < GameManager._nbJoueur; i++)
		{
			if (i != Id)
			{
				string[] attack = GameManager.InfoAutreJoueur[$"attack{i}"].Split("*");
				if (attack[0] == $"heal{Id}")
				{
					GameManager.InfoAutreJoueur[$"attack{i}"] = "";
					SetHealth(GameManager.Health + Conversions.AtoI(attack[1]));
				}
				else if (attack[0] == $"revive{Id}")
				{
					GameManager.InfoAutreJoueur[$"attack{i}"] = "";
					Revive();
				}
			}
		}
	}

	protected void Pause()
	{
		if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[16].Item2) && !GameManager._pausemode && _uiTimer > 20)
		{
			GameManager.InfoJoueur["animation"] = "idle";
			CameraV.SpringLength = -4;
			_uiTimer = 0;
			GameManager._pausemode = true;
			Input.MouseMode = Input.MouseModeEnum.Visible;
			PackedScene pauseUI = GD.Load<PackedScene>("res://Scenes/UI/PauseMenuManager.tscn");
			Control pauseMenu = pauseUI.Instantiate<Control>();
			GetParent().AddChild(pauseMenu);
		}
		else if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[16].Item2) && GameManager._pausemode && _uiTimer > 20)
		{
			_uiTimer = 0;
			GameManager._pausemode = false;
		}
	}
	
	protected void Inventory()
	{
		if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[12].Item2) && !GameHUD.OnInventory && _uiTimer > 20)
		{
			GameManager.InfoJoueur["animation"] = "idle";
			CameraV.SpringLength = -4;
			_uiTimer = 0;
			GameHUD.OnInventory = true;
			Input.MouseMode = Input.MouseModeEnum.Visible;
			AddChild(_inventory);
			
		}
		else if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[12].Item2) && GameHUD.OnInventory && _uiTimer > 20)
		{
			_uiTimer = 0;
			GameHUD.OnInventory = false;
			Input.MouseMode = Input.MouseModeEnum.Captured;
			RemoveChild(_inventory);
		}
	}

	protected void ActiveSkills()
	{
		if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[6].Item2))
		{
			ActiveSkill(0);
		}
		else if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[7].Item2))
		{
			ActiveSkill(1);
		}
		else if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[8].Item2))
		{
			ActiveSkill(2);
		}
	}

	private void ActiveSkill(int nbskill)
	{
		string skill = GameManager.Skills[nbskill].Item1;
		if (skill == "reviveall")
		{
			if (UseStamina(1000) && !GameManager.ReviveAll)
			{
				
				foreach (var player in GameManager.ListJoueur)
				{
					PlayerScript playerscripts = (PlayerScript)player;
					if (playerscripts.IsDead)
					{ 
						playerscripts.Revive();
					}
					player.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y + new Random().Next(2, 5), GlobalPosition.Z);
				}
			}
		}
		else if (skill == "invincibility")
		{
			if (GameHUD.Skills.IsCooldown(nbskill))
			{
				GameHUD.Skills.StartCooldown(nbskill, 250);
				Invincibility = true;
			}
		}
		else if (skill == "invisibility")
		{
			if (GameHUD.Skills.IsCooldown(nbskill))
			{
				GameHUD.Skills.StartCooldown(nbskill, 200);
				Visible = false;
			}
		}
		else if (skill == "agro")
		{
			if (GameHUD.Skills.IsCooldown(nbskill))
			{
				GameHUD.Skills.StartCooldown(nbskill, 100);
				Agro = true;
			}
		}
		else if (skill == "absorption")
		{
			if (GameHUD.Skills.IsCooldown(nbskill))
			{
				GameHUD.Skills.StartCooldown(nbskill, 80);
				Absorption = true;
			}
		}
	}

	protected void PhysicsReset()
	{
		MovementSpeed = 0f;
	}
	
	protected void Gravity(double delta)
	{
		if (!IsOnFloor())
		{
			VerticalVelocity += Vector3.Down * GravityValue * 2 * (float)delta;
		}
		else
		{
			VerticalVelocity = Vector3.Down * GravityValue / 10 * (float)delta;
		}
	}
	public override void Revive()
	{
		Position+= new Vector3(0,10,0);
		SetHealth(GetMaxHealth());
		SetStamina(GetMaxStamina());
		IsDead = false;
	}
	
	protected virtual void Move(double delta)
	{
		List<(string, Key)> controls = GameManager.InputManger.GetAllControl();
		if (Input.IsKeyPressed(controls[0].Item2) || Input.IsKeyPressed(controls[1].Item2) || Input.IsKeyPressed(controls[2].Item2) ||
			Input.IsKeyPressed(controls[3].Item2))
		{
			int left = Conversions.BtoI(Input.IsKeyPressed(controls[2].Item2));
			int right = Conversions.BtoI(Input.IsKeyPressed(controls[3].Item2));
			int forward = Conversions.BtoI(Input.IsKeyPressed(controls[0].Item2));
			int backward = Conversions.BtoI(Input.IsKeyPressed(controls[1].Item2));
					
			Direction = new Vector3(left - right, 0, forward - backward);
			Direction = Direction.Rotated(Vector3.Up, CameraH.Rotation.Y).Normalized();
			IsWalking = true;
			MovementSpeed = GameManager.WalkSpeed;
		}
			
		PlayerMesh.Rotation = new Vector3(0, CameraH.Rotation.Y + (float) Math.PI, 0);
			
		HorizontalVelocity = HorizontalVelocity.Lerp(Direction.Normalized() * MovementSpeed, (float)(Acceleration * delta));
		
		Vector3 velocity = Velocity;
		velocity.Z = HorizontalVelocity.Z + VerticalVelocity.Z;
		velocity.X = HorizontalVelocity.X + VerticalVelocity.X;
		velocity.Y = VerticalVelocity.Y;
		
		Velocity = velocity;
		MoveAndSlide();
	}
	
	public abstract void TakeDamage(int damage);
	
	public bool UseStamina(int stamina)
	{
		if (GameManager.Stamina >= stamina)
		{
			GameManager.Stamina -= stamina;
			return true;
		}

		return false;
	}
	
	public int GetHealth()
	{
		return GameManager.Health;
	}
	public void SetHealth(int health)
	{
		if (health > GameManager.MaxHealth)
		{
			GameManager.Health = GameManager.MaxHealth;
		}
		else
		{
			GameManager.Health = health;
		}
	}
	
	public int GetMaxHealth()
	{
		return GameManager.MaxHealth;
	}
	
	public int GetStamina()
	{
		return GameManager.Stamina;
	}
	
	public void SetStamina(int stamina)
	{
		if(stamina > GameManager.MaxStamina)
		{
			GameManager.Stamina =GameManager.MaxStamina;
		}
		else
		{
			GameManager.Stamina = stamina;
		}
	}
	
	public int GetMaxStamina()
	{
		return GameManager.MaxStamina;
	}
	private void _on_potion_timer_timeout()
	{
		GameManager.Health = GameManager.CurrentHealth;
		GameManager.MaxHealth = GameManager.CurrentMaxHealth;
		GameManager.WalkSpeed = GameManager.CurrentWalkSpeed;
		GameManager.RunSpeed = GameManager.CurrentRunSpeed;
	}
}
