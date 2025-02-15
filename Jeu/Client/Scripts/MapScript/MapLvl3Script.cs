using Godot;
using System;
using System.Collections.Generic;
using Lib;

public partial class MapLvl3Script : IMap
{
	/*
	debug les magic value pour le pont
	enlver les magic value
	recoder au propre le code
	autre
	*/
	// Called when the node enters the scene tree for the first time.
	private int MapLenght = 33;//Nombre impaire uniquement
	private int[,] MapGrid;
	private (int x,int y) S;
	private const int Gap = 30;
	private const int LenI = (32)/2;
	private NavigationRegion3D NavMesh = GD.Load<PackedScene>("res://Scenes/NavMesh.tscn").Instantiate<NavigationRegion3D>();
	public override void _Ready()
	{
		Ani = GetNode<AnimationPlayer>("Animation/AnimationPlayer"); 
		((NavMeshScript)NavMesh).InitNavMesh();
		AddChild(NavMesh);	
		MapGrid = new int[MapLenght,MapLenght];
		for (int i = 0; i < MapLenght; i++)
		{
			for (int j = 0; j < MapLenght; j++)
			{
				MapGrid[i, j] = 0;
			}
		}
		LoadingStage = "Create MapGrid";
		CreateMap(12,16,16);
		LoadingStage = "Create Map";
		CreateCity();
		LoadingStage = "Get SpawnPoint";
		S = GetSpawnBuilding();
		MapGrid[16, 16] = 2;
		MapGrid[S.x, S.y] = 2;
		PrintMatrix(MapGrid);
		LoadingStage = "Calculate NavMesh";
		((NavMeshScript)NavMesh).CreateNavMesh();
		SetUp = true;
		LoadingStage = "En attente des autres joueurs :(";
	}
	public override void _PhysicsProcess(double delta)
	{
		//GD.Print(LoadingStage);
		SyncCam();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		CanExit = true;
		if (!MapReady && (NavMesh as NavMeshScript).NavMeshReady)
		{
			MapReady = true;
			LoadingStage = "En attente des autres joueurs :(";
		}
		else
		{
			if (SkillsMenu)
			{
				Visible = false;
				SkillsMenu = false;
				ShowSkillsMenu();
			}
		}
	}

	public override List<(int, int, int)> GetSpawnLocation()
	{
		List<(int, int, int)> Res = new List<(int, int, int)>();
		
		Res.Add((((S.x-LenI)*Gap)+1,35,((S.y-LenI)*Gap)+1));
		Res.Add((((S.x-LenI)*Gap)-1,35,((S.y-LenI)*Gap)+1));
		Res.Add((((S.x-LenI)*Gap)+1,35,((S.y-LenI)*Gap)-1));
		Res.Add((((S.x-LenI)*Gap)-1,35,((S.y-LenI)*Gap)-1));
	
		return Res;
	}
	
	public override Vector3 GetEndLocation()
	{
		return new Vector3(0,0,0);
	}

	public override void DebugMode(CharacterBody3D Player, bool DebugMode)
	{
		MapTool.Debug(Player,this,DebugMode);
	}
	
	private void CreateMap(int n, int i, int j)
	{
		MapGrid[i,j] = 1;
		if(n>0)
		{
			for(int m = 0; m<2 ;m++)
			{
				List<(int,int)> PossibleAction = new List<(int,int)>();
				if(i<MapLenght-2 && MapGrid[i+2,j]==0)
				{
					PossibleAction.Add((2,0));
				}
				if(i>1 && MapGrid[i-2,j]==0)
				{
					PossibleAction.Add((-2,0));
				}
				if(j<MapLenght-2 && MapGrid[i,j+2]==0)
				{ 
					PossibleAction.Add((0,2));
				}
				if(j>1 && MapGrid[i,j-2]==0)
				{
					PossibleAction.Add((0,-2));
				}
				if(PossibleAction.Count!=0 && n!=1)
				{
					int random = Rand.Next(0,PossibleAction.Count);
					(int x,int y) = PossibleAction[random];
					if(x==0) MapGrid[i+(x)/2,j+(y)/2] = -1;
					else MapGrid[i+(x)/2,j+(y)/2] = -2;
					CreateMap(n-1,i+x,j+y);
				}
			}	
		}
	}
	private void PrintMatrix(int[,] matrix)
	{
		for (int i = 0; i < MapLenght; i++)
		{
			string res = "";
			for (int j = 0; j < MapLenght; j++)
			{
				switch (matrix[i, j])
				{
					case 0:
						res += " ";
						break;
					case 1:
						res += "o";
						break;
					case -1:
						res += "-";
						break;
					case -2:
						res += "|";
						break;
					default:
						res += "#";
						break;
				}
			}
		}
	}
	
	private void CreateCity()
	{
		for (int i = 0; i < MapLenght; i++)
		{
			for (int j = 0; j < MapLenght; j++)
			{
				switch (MapGrid[i, j])
				{
					case 0:
						int nb = Rand.Next(6,11);
						if(nb==11)
						{
							nb=3;
						}
						int rot = Rand.Next(0,4)*90;
						Node3D Bat0 = GD.Load<PackedScene>($"res://Ressources/Map/Moderne/Building/Building{nb}.tscn").Instantiate<Node3D>();
						Bat0.Position = new Vector3((i-LenI)*Gap,0,(j-LenI)*Gap);
						Bat0.Rotation = new Vector3(0,(float)Mathf.DegToRad(rot),0);
						AddChild(Bat0);
						break;
					case 1:
						Node3D Bat1 = GD.Load<PackedScene>("res://Ressources/Map/Moderne/Building/Building5.tscn").Instantiate<Node3D>();
						Bat1.Position = new Vector3((i-LenI)*Gap,0,(j-LenI)*Gap);
						AddChild(Bat1);
						MeshInstance3D Roof = Bat1.GetNode<MeshInstance3D>("Roof");
						Bat1.RemoveChild(Roof);
						Roof.Position+=Bat1.GlobalPosition;
						NavMesh.AddChild(Roof);
						break;
					case -1:
						Node3D Bat2 = GD.Load<PackedScene>("res://Ressources/Map/Moderne/Building/Bridge.tscn").Instantiate<Node3D>();
						Node3D R1 = Bat2.GetNode<Node3D>("Roof");
						Bat2.RemoveChild(R1);
						R1.Position = new Vector3((i-LenI)*Gap,R1.Position.Y,(j-LenI)*Gap);
						MeshInstance3D Roof2 = Bat2.GetNode<MeshInstance3D>("MRoof");
						Bat2.RemoveChild(Roof2);
						Roof2.Position = new Vector3((i-LenI)*Gap,R1.Position.Y,(j-LenI)*Gap);
						NavMesh.AddChild(Roof2);
						AddChild(R1);
						break;
					case -2:
						Node3D Bat3 = GD.Load<PackedScene>("res://Ressources/Map/Moderne/Building/Bridge.tscn").Instantiate<Node3D>();
						Node3D R2 = Bat3.GetNode<Node3D>("Roof2");
						Bat3.RemoveChild(R2);
						R2.Position = new Vector3((i-LenI)*Gap,R2.Position.Y,(j-LenI)*Gap);
						MeshInstance3D Roof3 = Bat3.GetNode<MeshInstance3D>("MRoof2");
						Bat3.RemoveChild(Roof3);
						Roof3.Position = new Vector3((i-LenI)*Gap,R2.Position.Y,(j-LenI)*Gap);
						NavMesh.AddChild(Roof3);
						AddChild(R2);
						break;
				}
			}
		}
	}
	
	private (int x,int y) GetSpawnBuilding()
	{
		(int x,int y) Spawn = (0,0);
		double Max = 0;
		for(int i = 0; i<MapLenght; i++)
		{
			for(int j = 0; j<MapLenght; j++)
			{
				if(MapGrid[i,j]==1)
				{
					double dist = MapTool.Distance(new Vector3(i,0,j),new Vector3(16,0,16));
					if (dist>Max)
					{
						Max = dist;
						Spawn = (i,j);
					}
				}
			}
		}
		return Spawn;
	}
}
