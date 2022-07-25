public class Game
{
	public string? Id { get; set; }
	public string? Name { get; set; }
	public GameType GameType { get; set; }
	public SaveRoot SaveRoot { get; set; }
	public string SaveRootSubdirectory { get; set; } = "";
	public string? GameDirectory { get; set; }
	public string? WinePrefix { get; set; }
	public string IncludeRegex { get; set; } = ".*";
	public string ExcludeRegex { get; set; } = "";

	public string FullPath
	{
		get
		{
			if(SaveRoot == SaveRoot.Custom) return SaveRootSubdirectory;

			string home = Environment.GetEnvironmentVariable("HOME")
				?? "/home/" + Environment.GetEnvironmentVariable("USER");

			string root = ".";

			switch (GameType)
			{
				case GameType.Native:
					switch (SaveRoot)
					{
						case SaveRoot.GameDirectory:
							root = GameDirectory ?? ".";
							break;
						case SaveRoot.Home:
							root = home;
							break;
						case SaveRoot.Documents:
							root = Environment.GetEnvironmentVariable("XDG_DOCUMENTS_DIR")
								?? home + "/Documents";
							break;
						case SaveRoot.XDGDataHome:
							root = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
								?? home + "/.local/share";
							break;
						case SaveRoot.XDGConfigHome:
							root = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
								?? home + "/.config";
							break;
						default:
							throw new Exception("Unknown save root " + SaveRoot + " for game type " + GameType);
					}
					break;
				case GameType.Wine:
					string pfx = WinePrefix ?? home + ".wine";
					// TODO: enviroment variables
					string profile = pfx + "/drive_c/users/" + Environment.GetEnvironmentVariable("USER");
					switch (SaveRoot)
					{
						case SaveRoot.Home:
							root = profile;
							break;
						case SaveRoot.Documents:
							root = profile + "/My Documents";
							break;
						case SaveRoot.WineAppDataLocal:
							root = profile + "/AppData/Local";
							break;
						case SaveRoot.WineAppDataLocalLow:
							root = profile + "/AppData/LocalLow";
							break;
						case SaveRoot.WineAppDataLocalRoaming:
							root = profile + "/AppData/Roaming";
							break;
						default:
							throw new Exception("Unknown save root " + SaveRoot + " for game type " + GameType);
					}
					break;
				default:
					throw new Exception("Unknown game type: " + GameType);
			}

			return root + "/" + SaveRootSubdirectory;
		}
	}
}

public enum GameType
{
	Native,
	Wine
}

public enum SaveRoot
{
	/// The directory the game is installed in
	GameDirectory,
	/// Native: $HOME
	/// Wine: %USERPROFILE%
	Home,
	/// Native: $XDG_DOCUMENTS_DIR
	/// Wine: %USERPROFILE%/My Documents/
	Documents,
	/// Wine only
	/// %USERPROFILE%/AppData/Local/
	WineAppDataLocal,
	/// Wine only
	/// %USERPROFILE%/AppData/Local/
	WineAppDataLocalLow,
	/// Wine only
	/// %APPDATA%
	WineAppDataLocalRoaming,
	/// Native only
	/// $XDG_CONFIG_HOME
	XDGDataHome,
	/// Native only
	/// $XDG_CONFIG_DIRS
	XDGConfigHome,
	/// Uses subdirectory as the full path
	Custom
}