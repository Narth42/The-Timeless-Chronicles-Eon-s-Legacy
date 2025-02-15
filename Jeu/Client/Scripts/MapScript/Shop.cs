using Godot;
using System;
using System.Collections.Generic;
using Lib;

public partial class Shop : IMap
{
	private Node3D Emax;
	private AnimationPlayer EmaxAnimation;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Ani = GetNode<AnimationPlayer>("Animation/AnimationPlayer");
		Emax = GetNode<Node3D>("Shop/Emax");
		EmaxAnimation = Emax.GetNode<AnimationPlayer>("AnimationPlayer");
		EmaxAnimation.Play("Animation");
		Ani.Play("Enter");
		(int c1,int c2) = SaveDialogue.Emax;
		c1++;
		SaveDialogue.Emax = (c1,c2);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		CanExit = true;
		SyncCam();
		if (!MapReady)
		{
			MapReady = true;
			LoadingStage = "En attente des autres joueurs :(";
			
		}
		
	}

	public override List<(int, int, int)> GetSpawnLocation()
	{
		List<(int,int,int)> res = new List<(int,int,int)>();
		res.Add((2,0,0));
		res.Add((0,0,2));
		res.Add((-2,0,0));
		res.Add((0,0,-2));
		
		return res;
	}
	
	public override Vector3 GetEndLocation()
	{
		return new Vector3(0,0,48);
	}


	public override void DebugMode(CharacterBody3D Player, bool DebugMode)
	{
		MapTool.Debug(Player,this,DebugMode);
	}

}
