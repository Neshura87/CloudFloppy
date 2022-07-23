using System.Net;
using System.Xml.Linq;
using System.Text.Json;
using WebDav;


namespace GameSync;

class Nextcloud
{
	private IWebDavClient nextCloudClient;
	private NetworkCredential nextcloudCredentials;

	private string userConfigPath = "config.json";
	private Config userConfig = new Config();

	private Uri uri;

	public Nextcloud()
	{
		initConfig();
		initClient();
		makeApiCall(userConfig.nextcloud.username + "/");
	}

	private void initConfig()
	{
		if (File.Exists(userConfigPath))
		{
			bool changes = false;
			string jsonString = File.ReadAllText(userConfigPath);
			if (string.IsNullOrWhiteSpace(jsonString))
			{
				getCredentials();
				getUrl();
				saveConfig();
			}
			else
			{
				if (!jsonString.StartsWith("{") && !jsonString.StartsWith("["))
				{
					getCredentials();
					getUrl();
					saveConfig();
				}
				else
				{
					try
					{
						userConfig = JsonSerializer.Deserialize<Config>(jsonString)!;
						if (!userConfig.nextcloud.IsNullOrEmpty())
						{
							if (userConfig.nextcloud.hasCredentials())
							{
								nextcloudCredentials = userConfig.nextcloud.generateCredentials();
							}
							else
							{
								getCredentials();
								changes = true;
							}

							if (userConfig.nextcloud.hasUrl())
							{
								uri = new Uri(userConfig.nextcloud.url);
							}
							else
							{
								getUrl();
								changes = true;
							}
						}
						else
						{
							getCredentials();
							getUrl();
							changes = true;
						}

						if (changes)
						{
							saveConfig();
						}
						else { }
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.ToString());
					}
				}
			}
		}
		else
		{
			getCredentials();
			getUrl();
			saveConfig();
		}
	}
	private void getUrl()
	{
		Console.WriteLine("Enter Nextcloud URL:");
		userConfig.nextcloud.url = Console.ReadLine();
		uri = new Uri(userConfig.nextcloud.url);
	}

	private void getCredentials()
	{
		Console.WriteLine("Enter Nextcloud Username:");
		userConfig.nextcloud.username = Console.ReadLine();

		Console.WriteLine("Enter Nextcloud Password:");
		userConfig.nextcloud.plainPassword = Console.ReadLine();
		nextcloudCredentials = userConfig.nextcloud.generateCredentials();
	}
	private void saveConfig()
	{
		Console.WriteLine("Do you want to save your config? [Y/N]");
		string answer = Console.ReadLine();
		if (answer != "Y" && answer != "y")
		{
			return;
		}
		else
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			string jsonString = JsonSerializer.Serialize(userConfig, options);
			File.WriteAllText(userConfigPath, jsonString);
			// write userConfig as JSON to file
		}
	}
	private void makeApiCall(string path)
	{
		PropfindResponse res = nextCloudClient.Propfind(path).Result;

		foreach (WebDavResource entry in res.Resources)
		{
			Console.WriteLine("## " + entry.Uri + "|" + entry.ContentType + "|" + entry.LastModifiedDate + " ##");
		}
	}

	private void initClient()
	{
		WebDavClientParams webParams = new WebDavClientParams
		{
			BaseAddress = uri,
			Credentials = nextcloudCredentials
		};
		nextCloudClient = new WebDavClient(webParams);
	}
}
