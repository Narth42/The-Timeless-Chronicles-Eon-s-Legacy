using Godot;
using Lib;

public partial class State6 : GameManager
{
	public static void State(double delta)
	{
		if (!Quit)
			UDP.Send(soc2,InfoJoueur["id"] + "_" + "in:co:" + InfoJoueur["co"] + "/" + InfoJoueur["orientation"] + "/" + InfoJoueur["hp"] + "/" + InfoJoueur["mp"],iep2);
		

		if (!_pausemode && Lib.Conversions.ItoB(SettingsManager.GetAllSettings()["enableChat"]))
		{
			Chat();
		}
		else
		{
			_chat.Visible = false;
		}
		
		if (InfoJoueur["attack"] != "")
		{
			UDP.Send(soc2,InfoJoueur["id"] + "_" + "on:" + InfoJoueur["attack"],iep2);
			InfoJoueur["attack"] = "";
		}

		if (InfoJoueur["animation"] != "")
		{
			UDP.Send(soc2,InfoJoueur["id"] + "_" + "an:" + InfoJoueur["animation"],iep2);
			InfoJoueur["animation"] = "";
		}

		if (InfoJoueur["ia"] != "")
		{
			UDP.Send(soc2,InfoJoueur["id"] + "_" + "ia:" + InfoJoueur["ia"],iep2);
			InfoJoueur["ia"] = "";
		}

		if (InfoJoueur["boss"] != "")
		{
			UDP.Send(soc2,InfoJoueur["id"] + "_" + "bo:" + InfoJoueur["boss"],iep2);
			InfoJoueur["boss"] = "";
		}
	}

	private static void Chat()
	{
		_chat.Visible = true;
		string commandchat = ((ChatUI)_chat).Inputtext;
		if (commandchat != "")
		{
			if (commandchat[0] == '/')
			{
				commandchat = commandchat.Substring(1);

				if (Cheat)
				{
					if (commandchat == "debug")
					{
						DebugMode = !DebugMode;
						((IMap)Map).DebugMode(Joueur1,DebugMode);
						
					}
					else if (commandchat == "display")
					{
						CDisplay = !CDisplay;
						((IMap)Map).SetUpCursor(Joueur1,CDisplay);
					}
					else if (commandchat.Length > 3 && commandchat.Substring(0,4) == "help")
					{
						if (commandchat.Length == 4)
						{
							((ChatUI)_chat).Outputaddtext = "Effectuez \"/help <numero de page>\" pour charger une page ou \"/help <commande>\" pour de l'aide sur une commande précise";
						}
						else
						{
							commandchat = commandchat.Substring(5);
							if (commandchat == "1")
							{
								((ChatUI)_chat).Outputaddtext = "Page 1 : \n\tdebug - passe en camera debug\n\tdisplay - affiche les infos utiles\n\tsetrule - change une règle du jeu";
							}
							else if (commandchat == "setrule")
							{
								((ChatUI)_chat).Outputaddtext = "setrule : \n\tfog <on/off> - active ou désactive le fog";
							}
							else
							{
								((ChatUI)_chat).Outputaddtext = "Effectuez \"/help <numero de page>\" pour charger une page ou \"/help <commande>\" pour de l'aide sur une commande précise";
							}
						}
					}
					else if (commandchat.Length >= 8 && commandchat.Substring(0, 7) == "setrule")
					{
						commandchat = commandchat.Substring(8);
						if (commandchat.Length >= 4 && commandchat.Substring(0,3) == "fog")
						{
							commandchat = commandchat.Substring(4);
							if (commandchat == "off")
							{
								Fog = false;
							}
							else if (commandchat == "on")
							{
								Fog = true;
							}
						}
						if (commandchat.Substring(0,2) == "ia")
						{
							commandchat = commandchat.Substring(4);
							if (commandchat.Substring(0,3) == "off")
							{
								DebugMode = DebugMode;
							}
							else if (commandchat.Substring(0,2) == "on")
							{
								DebugMode = DebugMode;
							}
						}
					}

					else if (commandchat == "end")
					{
						Joueur1.Position = Map.GetEndLocation();
					}
					
					else if (commandchat == "next")
					{
						InfoJoueur["attack"] = "next";
					}
					
				}
				else if (commandchat == "cheat on")
				{
					Cheat = true;
					((ChatUI)_chat).Outputaddtext = "passage en mode triche";

				}
				else if (commandchat == "help")
				{
					((ChatUI)_chat).Outputaddtext = "effectuez \"/cheat on\" pour passer en mode triche";
				}
				
				string[] line = commandchat.Split(" ");
				if (line[0] == "give")
				{
					if (line[1] == "gold")
					{
						GameManager.Gold+= int.Parse(line[2]);
					}
					else if (line[1] == "xp")
					{
						GameManager.xp+= int.Parse(line[2]);
					}
				}
			}
			else
			{
				UDP.Send(soc2,InfoJoueur["id"] + "_" + "chat:" + ((ChatUI)_chat).Inputtext,iep2);
			}
			((ChatUI)_chat).Inputtext = "";
		}
	}
}
