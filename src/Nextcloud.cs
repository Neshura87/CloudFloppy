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
		// set used URL to include username to reduce complexity during API calls
		userConfig.nextcloud.url = Console.ReadLine() + userConfig.nextcloud.username + "/";
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
	public List<NextcloudObject> Find(string path)
	{
		List<NextcloudObject> found = new List<NextcloudObject>();
		var propfindParams = new PropfindParameters
		{
			RequestType = PropfindRequestType.NamedProperties,
			CustomProperties = new XName[] {
					"{DAV:}getlastmodified",
					"{DAV:}getetag",
					"{http://owncloud.org/ns}id",
					"{http://owncloud.org/ns}fileid"
				},
			Namespaces = new[] {
					new NamespaceAttr("oc", "http://owncloud.org/ns")
				}
		};
		var call = nextCloudClient.Propfind(path, propfindParams);
		//call.Start();
		while (!call.IsCompleted)
		{
			Console.Write(".");
			Thread.Sleep(200);
		}
		Console.WriteLine();

		PropfindResponse res = call.Result;

		foreach (WebDavResource entry in res.Resources)
		{
			NextcloudObject obj = new NextcloudObject(entry);
			found.Add(obj);
		}
		return found;
	}

	public void Sync(System.IO.FileInfo file)
	{
		//propfing file with same name
		Find(file.Name);
		//if not found upload
		//else downlaod
	}

	/*public NextcloudObject Get(string path)
    {

        return ;
    }*/

	public void setSavePath(string path)
	{
		userConfig.nextcloud.path = path;
	}

	public class NextcloudObject
	{
		public WebDavResource Entry;
		public string Name;
		public string Type;
		public string Path;
		public NextcloudObject(WebDavResource obj)
		{
			Entry = obj;
			Path = Entry.Uri;

			if (Path.EndsWith("/"))
			{
				Type = "Directory";
			}
			else
			{
				Type = "File";
			}

			Name = extractName();
		}

		private string extractName()
		{
			string name = Path;
			name = name.TrimEnd('/');
			string[] parts = name.Split('/');
			name = parts.Last();
			return name;
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
