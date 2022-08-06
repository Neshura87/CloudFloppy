using System.Text.Json;


namespace CloudFloppy;

public class Config
{
	public static Config Instance;

	public static bool LoadConfig()
	{
		string home = Environment.GetEnvironmentVariable("HOME")
				?? "/home/" + Environment.GetEnvironmentVariable("USER");

		string xdgconfdir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
								?? home + "/.config";

		string confpath = xdgconfdir + "/CloudFloppy/config.json";

		if (!File.Exists(confpath)) return false;

		Config.Instance = JsonSerializer.Deserialize<Config>(File.ReadAllText(confpath));
		return true;
	}

	public void saveConfig()
	{
		string home = Environment.GetEnvironmentVariable("HOME")
				?? "/home/" + Environment.GetEnvironmentVariable("USER");

		string xdfconfdir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
								?? home + "/.config";

		string confpath = xdfconfdir + "/CloudFloppy/config.json";

		Directory.CreateDirectory(Path.GetDirectoryName(confpath));

		File.WriteAllText(confpath, JsonSerializer.Serialize(this));
	}

	public Config()
	{
		Nextcloud = new();
		Rsync = new();
	}
	public string Provider { get; set; }
	public NextcloudConfig Nextcloud { get; set; }
	public RsyncConfig Rsync { get; set; }
	public List<Game> Games { get; set; } = new List<Game>();

	public bool IsNullOrEmpty()
	{
		if (Nextcloud.IsNullOrEmpty())
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	public void AddGame(Game game)
	{
		Games.Add(game);
	}

	public void RemoveGame(string gameId)
	{
		Games.RemoveAll(g => g.Id == gameId);
	}

	public Task ModifyGame(Game game, string property, string newValue)
	{
		throw new NotImplementedException();
	}
}