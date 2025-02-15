using Godot;
using System;
using JeuClient.Scripts.PlayerScripts;
using Lib;

public partial class ScientistScript : ClassScript
{
	//Varibale du tir
	private int _shootCooldown;
	private bool _isShooting;
	private bool _shootAnimation;
	
	private int _shootCooldownValue = 100;
	public static bool IsAiming;
	
	private Timer _manaTimer;
	private Timer _reloadTimer;
	private bool _reload;

	public bool ReloadProtection;
	public bool ReviveAll;
	
	public override void _Ready()
	{
		InitPlayer();

		_shootCooldown = _shootCooldownValue - 50;
		_manaTimer = GetNode<Timer>("StaminaTimer");
		_reloadTimer = GetNode<Timer>("ReloadTimer");
		
		foreach (var skill in GameManager.Skills)
		{
			if (skill.Item1 == "reloadprotection")
			{
				ReloadProtection = true;
			}
			else if (skill.Item1 == "reviveall")
			{
				ReviveAll = true;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (Camera.Current && !GameManager._pausemode)
		{
			Zoom(@event);
		}
	}

	public override void _Process(double delta)
	{
		SendPosition();
		HeathPlayer();
	}

	public override void _PhysicsProcess(double delta)
	{
		_uiTimer += 1;
		
		Pause();
		PhysicsReset();
		Gravity(delta);
		
		if (!IsDead)
		{
			if (Camera.Current && !GameManager._pausemode && !((ChatUI)GameManager._chat).IsOnChat())
			{
				Inventory();
				ShootLaser();
				Animation();
				ActiveSkills();
				if (Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[13].Item2) && !(Input.IsMouseButtonPressed(MouseButton.Left) && IsAiming))
				{
					if (!_reload)
					{
						_reloadTimer.Start();
						_reload = true;
					}
				}
				else
				{
					if (_reload)
					{
						_reloadTimer.Stop();
						_reload = false;
					}
				}
				
				if (!_isShooting && !_reload)
				{
					Move(delta);
				}
			}
			else
			{
				if (_shootCooldown >= _shootCooldownValue)
				{
					_shootCooldown = _shootCooldownValue - 20;
				}
				
				if (AnimationState != 0)
				{
					AnimationState = 0;
					AnimationSet(false, false, true);
					GameManager.InfoJoueur["animation"] = "idle";
				}
				
				Velocity = new Vector3(0, 0, 0);
			}
		}
	}
	
	private void ShootLaser()
	{
		_shootCooldown += 1;
		
		if (CameraV.SpringLength > -4 && !IsAiming)
		{
			CameraV.SpringLength -= 0.1f;
		}
		
		if (Input.IsMouseButtonPressed(MouseButton.Right))
		{
			if (CameraV.SpringLength <= -1.5f)
			{
				CameraV.SpringLength += 0.1f;
			}
			
			IsAiming = true;
			
			PackedScene crossHairScene = GD.Load<PackedScene>("res://Scenes/HUD/ViewFinder.tscn");
			Control crossHair = crossHairScene.Instantiate<Control>();
			AddChild(crossHair);
		}
		else if (IsAiming && !_isShooting)
		{
			IsAiming = false;
		}
		
		if (Input.IsMouseButtonPressed(MouseButton.Left) && _shootCooldown > _shootCooldownValue && IsAiming && !_isShooting && UseStamina(GameManager.ManaUse / 4))
		{
			PackedScene laserScene = GD.Load<PackedScene>("res://Scenes/EntityScenes/Laser.tscn");
			Node3D laser = laserScene.Instantiate<Node3D>();
			
			double rotationY = CameraH.Rotation.Y;
			Vector3 laserPosition = new Vector3(CameraV.GlobalPosition.X + (float)Math.Sin(rotationY), Position.Y + 1.2f, Position.Z + (float)Math.Cos(rotationY));
			laser.GlobalPosition = new Vector3((laserPosition.X + GlobalPosition.X) / 2, laserPosition.Y, (laserPosition.Z + GlobalPosition.Z) / 2);
			laser.Rotation = new Vector3(CameraV.Rotation.X + 0.15f, (float)rotationY, CameraV.Rotation.X + 0.15f);
			UDP.OneShot($"{laser.Position.X};{laser.Position.Y};{laser.Position.Z};{laser.Rotation.X};{laser.Rotation.Y};{laser.Rotation.Z}");
			GetTree().Root.AddChild(laser);
			
			GameManager.LockCamera = true;
			_manaTimer.Start();
		}
		
		if (!Input.IsMouseButtonPressed(MouseButton.Left) && (_isShooting || GameManager.Stamina <= 0))
		{
			GameManager.InfoJoueur["attack"] = "stop";
			GameManager.InfoJoueur["animation"] = "stop";
			_isShooting = false;
			GameManager.LockCamera = false;
			_manaTimer.Stop();
		}
	}
	
	private void Animation()
	{
		bool left = Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[2].Item2);
		bool right = Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[3].Item2);
		bool forward = Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[0].Item2);
		bool backward = Input.IsKeyPressed(GameManager.InputManger.GetAllControl()[1].Item2);
		
		(int, int) direction = (Conversions.BtoI(left) - Conversions.BtoI(right), Conversions.BtoI(forward) - Conversions.BtoI(backward));

		if (Input.IsMouseButtonPressed(MouseButton.Left) && _shootCooldown > _shootCooldownValue && IsAiming && !_isShooting && AnimationState != 2 && AnimationState != -2 && !InteractionShop.OnShop && !GameHUD.OnInventory)
		{
			DirectionControl = (0,0);
			AnimationState = 2;
			
			AnimationSet(false, true, false);
			
			GameManager.InfoJoueur["animation"] = "shoot";
			
			_shootCooldown = 0;
			_isShooting = true;
		}
		else if (!(Input.IsMouseButtonPressed(MouseButton.Left) && IsAiming) && _reload && AnimationState != 3 && AnimationState != -2 && !InteractionShop.OnShop && !GameHUD.OnInventory)
		{
			AnimationState = 3;
			DirectionControl = (0,0);
			AnimationSet(false, false, false, false, false, true);
			
			GameManager.InfoJoueur["animation"] = "reload";
		}
		else if ((left || right || forward || backward) && direction != DirectionControl && AnimationState != -2)
		{
			AnimationState = 1;
			DirectionControl = direction;
			AnimationTree.Set("parameters/Walk/blend_position", new Vector2(direction.Item1, direction.Item2));
			
			AnimationSet(true, false, false);

			if (direction.Item2 != 0)
			{
				GameManager.InfoJoueur["animation"] = "walk";
			}
			else
			{
				GameManager.InfoJoueur["animation"] = "walkside";
			}
		}
		else if (!(Input.IsMouseButtonPressed(MouseButton.Left) && IsAiming) && (!(left || right || forward || backward) || AnimationState != 1) && AnimationState != 0 && AnimationState != -2 && !_reload)
		{
			AnimationState = 0;
			DirectionControl = (0,0);
			AnimationSet(false, false, true);
			
			GameManager.InfoJoueur["animation"] = "idle";
		}
	}
	
	private void AnimationSet(bool walk, bool shoot, bool idle, bool damage = false, bool death = false, bool reload = false)
	{
		AnimationTree.Set("parameters/conditions/WhenWalk", walk);
		AnimationTree.Set("parameters/conditions/WhenShoot", shoot);
		AnimationTree.Set("parameters/conditions/Idle", idle);
		AnimationTree.Set("parameters/conditions/Death", death);
		AnimationTree.Set("parameters/conditions/Damage", damage);
		AnimationTree.Set("parameters/conditions/WhenReload", reload);
	}
	
	public override void TakeDamage(int damage)
	{
		if (ReloadProtection && _reload)
		{
			GameManager.Health -= damage / 2;
		}
		else
		{
			GameManager.Health -= damage;	
		}
		
		if (GameManager.Health <= 0 && !IsDead)
		{
			IsDead = true;
			AnimationState = -1;
			AnimationSet(false, false, false, false, true);
			GameManager.InfoJoueur["animation"] = "death";
			GetNode<Timer>("DeathTimer").Start();
		}
		else
		{
			AnimationState = -2;
			AnimationSet(false, false, false, true);
			GameManager.InfoJoueur["animation"] = "damage";
			DamageTimer.Start();
		}
	}
	
	private void _on_stamina_timeout()
	{
		UseStamina(GameManager.ManaUse / 6);
	}
	
	private void _on_death_timer_timeout()
	{
		Position -= new Vector3(0,10,0);
	}
	
	private void _on_damage_timer_timeout()
	{
		AnimationState = -3;
	}

	private void _on_reload_timer_timeout()
	{
		if (GameManager.Stamina + GameManager.ChargeSpeed <= GameManager.MaxStamina)
		{
			GameManager.Stamina += GameManager.ChargeSpeed;
		}
	}
}
