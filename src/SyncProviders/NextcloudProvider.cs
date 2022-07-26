using System.Net;
using System.Xml.Linq;
using System.Text.Json;
using WebDav;


namespace GameSync;

class NextcloudSyncProvider : SyncProvider
{
    private IWebDavClient nextCloudClient;

    private NetworkCredential nextcloudCredentials;

    private string userConfigPath = "config.json";

    private Config userConfig = new Config();

    private Uri uri;

    public NextcloudSyncProvider()
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
                getPath();
                changes = true;
            }
            else
            {
                if (!jsonString.StartsWith("{") && !jsonString.StartsWith("["))
                {
                    getCredentials();
                    getUrl();
                    getPath();
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

                            if (!userConfig.nextcloud.hasPath())
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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                if (changes) saveConfig();
            }
        }
        else
        {
            getCredentials();
            getUrl();
            getPath();
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

    private void getPath()
    {
        Console.WriteLine("Enter relative Path for saves on Cloud:");
        userConfig.nextcloud.path = Console.ReadLine();
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
    public List<string> Propfind(string path)
    {
        List<string> found = new List<string>();
        var res = nextCloudClient.Propfind(path).Result;

        foreach (WebDavResource entry in res.Resources)
        {
			if(entry.Uri.TrimStart('/') != path) found.Add(entry.Uri.TrimStart('/'));
        }
        return found;
    }

    public void setSavePath(string path)
    {
        userConfig.nextcloud.path = path;
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

    public override async Task<DateTime?> GetLastSyncTime(string gameId)
    {
        DateTime? tmp = new DateTime?();
        return tmp;
    }

    public override async Task<List<string>> ListFiles(string gameId)
    {
        // needs to work recursively
        // returns relative path from saveroot (use gameId for that)
        // recursive due to fodlers
        List<string> contents = getContents("remote.php/dav/files/" + userConfig.nextcloud.username + "/" + userConfig.nextcloud.path + gameId + "/");
        return contents;
    }

    public override async Task DownloadFiles(string gameId, string outDir)
    {
        return;
    }

    public override async Task UploadFiles(string gameId, string inDir, DateTime lastModTime)
    {
        return;
    }

    public override async Task<SpaceUssage> GetSpaceUsage()
    {
        SpaceUssage tmp = new SpaceUssage();
        return tmp;
    }



}
