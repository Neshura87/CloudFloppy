using System.Net;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using WebDav;


namespace GameSync;

public class NextcloudConfig
{
	public NextcloudConfig()
	{
		_password = new byte[0];
		Username = "";
		Url = "";
		Path = "";
	}
	// key generated using random numbers
    [JsonIgnore]
	private static byte[] key =
				{
					0x65, 0xcd, 0xdd, 0x5d, 0x56, 0xd6, 0xc7, 0x22,
					0xbd, 0xdf, 0xd4, 0xa6, 0x6f, 0x1e, 0xe1, 0xdd
					};

	[JsonIgnore]
	private byte[] _password;

	public byte[] Password
	{
		get
		{
			return _password;
		}
		set
		{
			// this is needed for loading of the encrypted password
			_password = value;
		}
	}

	[JsonIgnore]
	public string plainPassword
	{
		get
		{
			return decryptPassword(_password);
		}
		set
		{
			// encrypt string so it can be safely stored
			_password = encryptPassword(value);
		}

	}
	public string Username { get; set; }
	public string Url { get; set; }

	public string Path { get; set; }

	public bool IsNullOrEmpty()
	{
		if (hasCredentials() || hasUrl())
		{ return false; }
		else { return true; }
	}

	public bool hasCredentials()
	{
		if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(Username))
		{ return false; }
		else { return true; }
	}

	public bool hasUrl()
	{
		return !string.IsNullOrEmpty(Url);
	}

	public NetworkCredential generateCredentials()
	{
		if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(plainPassword)) { }
		NetworkCredential auth = new NetworkCredential(Username, plainPassword);
		return auth;
	}

	private static byte[] encryptPassword(string input)
	{
		using (Aes aes = Aes.Create())
		{
			var stream = new MemoryStream();
			aes.Key = key;

			byte[] iv = aes.IV;
			stream.Write(iv, 0, iv.Length);

			using (CryptoStream cryptoStream = new(
							stream,
							aes.CreateEncryptor(),
							CryptoStreamMode.Write))
			{
				using (StreamWriter encryptWriter = new(cryptoStream))
				{
					encryptWriter.Write(input);
				}
			}

			return stream.ToArray();
		}
	}

	private static string decryptPassword(byte[] input)
	{
		if (input.Length == 0) { return ""; }
		using (Aes aes = Aes.Create())
		{
			var stream = new MemoryStream(input);
			aes.Key = key;

			byte[] iv = new byte[aes.IV.Length];
			int numBytesToRead = aes.IV.Length;
			int numBytesRead = 0;
			while (numBytesToRead > 0)
			{
				int n = stream.Read(iv, numBytesRead, numBytesToRead);
				if (n == 0) break;

				numBytesRead += n;
				numBytesToRead -= n;
			}


			using (CryptoStream cryptoStream = new(
			stream,
			aes.CreateDecryptor(key, iv),
			CryptoStreamMode.Read))
			{
				using (StreamReader decryptReader = new(cryptoStream))
				{
					return decryptReader.ReadToEndAsync().Result;
				}
			}
		}

	}
}

class NextcloudSyncProvider : SyncProvider
{
    private IWebDavClient nextCloudClient;

    private NetworkCredential nextcloudCredentials;

    private Uri uri;

    public NextcloudSyncProvider()
    {
        initConfig();
        initClient();
    }

    private void initConfig()
    {
        bool changes = false;
        if (!Config.Instance.Nextcloud.IsNullOrEmpty())
        {
            if (Config.Instance.Nextcloud.hasCredentials())
            {
                nextcloudCredentials = Config.Instance.Nextcloud.generateCredentials();
            }
            else
            {
                getCredentials();
                changes = true;
            }

            if (Config.Instance.Nextcloud.hasUrl())
            {
                uri = new Uri(Config.Instance.Nextcloud.Url);
            }
            else
            {
                getUrl();
                changes = true;
            }

            if (!Config.Instance.Nextcloud.hasPath())
            {
                getPath();
                changes = true;
            }
        }
        else
        {
            getCredentials();
            getUrl();
            getPath();
            changes = true;
        }

        if (changes) saveConfig();
    }

    private void getUrl()
    {
        Console.WriteLine("Enter Nextcloud URL:");
        Config.Instance.Nextcloud.Url = Console.ReadLine();
        uri = new Uri(Config.Instance.Nextcloud.Url);
    }

    private void getCredentials()
    {
        Console.WriteLine("Enter Nextcloud Username:");
        Config.Instance.Nextcloud.Username = Console.ReadLine();

        Console.WriteLine("Enter Nextcloud Password:");
        Config.Instance.Nextcloud.plainPassword = Console.ReadLine();
        nextcloudCredentials = Config.Instance.Nextcloud.generateCredentials();
    }

    private void getPath()
    {
        Console.WriteLine("Enter relative Path for saves on Cloud:");
        Config.Instance.Nextcloud.Path = Console.ReadLine();
    }

    private void saveConfig()
    {
        /*Console.WriteLine("Do you want to save your config? [Y/N]");
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
        }*/
        throw new NotImplementedException();
    }
    public List<string> Propfind(string path)
    {
        List<string> found = new List<string>();
        var res = nextCloudClient.Propfind(path).Result;

        foreach (WebDavResource entry in res.Resources)
        {
            if (entry.Uri.TrimStart('/') != path) found.Add(entry.Uri.TrimStart('/'));
        }
        return found;
    }

    public void setSavePath(string path)
    {
        Config.Instance.Nextcloud.Path = path;
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

    private List<string> getContents(string path)
    {
        List<string> contents = Propfind(path);
        List<string> delete = new List<string>();
        contents.Remove(path);
        for (int i = 0; i < contents.Count; i++)
        {
            // removes otherwise duplicate / in query
            if (contents[i].EndsWith("/"))
            {
                var deeper = getContents(contents[i]);
                foreach (string element in deeper)
                {
                    contents.Add(element);
                }
                // deleting content during the for loop would mess up the counting and possibly lead to duplicate entries
                delete.Add(contents[i]);
            }
        }
        foreach (string element in delete)
        {
            contents.Remove(element);
        }
        return contents;
    }
    // SyncProvider Functions implemented here

    public override Task<DateTime?> GetLastSyncTime(string gameId)
    {
        throw new NotImplementedException();
    }

    public override Task<List<string>> ListFiles(Game game)
    {
        // needs to work recursively
        // returns relative path from saveroot (use gameId for that)
        // recursive due to fodlers
        List<string> contents = getContents("remote.php/dav/files/" + Config.Instance.Nextcloud.Username + "/" + Config.Instance.Nextcloud.Path + game.Id + "/");
        return Task.FromResult(contents);
    }

    public override Task DownloadFiles(Game game)
    {
        throw new NotImplementedException();
    }

    public override Task UploadFiles(Game game, DateTime lastModTime)
    {
        throw new NotImplementedException();
    }

    public override Task<SpaceUssage> GetSpaceUsage()
    {
        throw new NotImplementedException();
    }



}
