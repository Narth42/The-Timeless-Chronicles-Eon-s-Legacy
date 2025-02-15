using Godot;
using System;
using Lib;
public abstract partial class IRender : Node3D
{
	private CharacterBody3D Player = new CharacterBody3D();
	private Vector3 Me = new Vector3();
	private Vector3 PlayerPos = new Vector3();
	private bool PlayerSet = false;
	protected int RenderDist = 150;
	protected Node3D Parent;
	[Export] public bool DoRender = true;
	protected bool RenderSetup()
	{
		if(DoRender)
		{
			Parent = (Node3D)GetParent().GetParent();
			if(!PlayerSet && GameManager.Joueur1!=null && Parent.IsAncestorOf(GameManager.Joueur1))
			{
				Player = GameManager.Joueur1;
				PlayerSet = true;
			}
			return PlayerSet;
		}
		else
		{
			return true;
		}
		
	}
	protected void Render()
	{
		if(DoRender)
		{
			Me = Position;
			PlayerPos = Player.Position;
			double dist = MapTool.Distance(Me,PlayerPos);
			if(dist>RenderDist)
			{
				Visible = false;
			}
			else
			{
				Visible = true;
			}
		}
	}
}
