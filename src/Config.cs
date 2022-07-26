using System.Text.Json;


namespace GameSync;

public class Config
{
	public static Config Instance;

	public static void LoadConfig()
	{
		string home = Environment.GetEnvironmentVariable("HOME")
				?? "/home/" + Environment.GetEnvironmentVariable("USER");

		string xdgconfdir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
								?? home + "/.config";

		string confpath = xdgconfdir + "/GameSync/config.json";

		if (!File.Exists(confpath)) throw new Exception("Config file not found: " + confpath);

		Config.Instance = JsonSerializer.Deserialize<Config>(File.ReadAllText(confpath));
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
}