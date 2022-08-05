using System.Net;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;


namespace CloudFloppy;

public class StatusCodeException : Exception
{
    public StatusCodeException(HttpStatusCode statusCode) :
        base("HTTP Status Code is invalid: " + statusCode.ToString())
    { }
}

public class NextcloudConfig
{
    public NextcloudConfig()
    {
        _password = new byte[0];
        Username = "";
        Url = "";
        SaveDir = "";
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

    public string SaveDir { get; set; }

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

    public bool hasPath()
    {
        return !string.IsNullOrEmpty(SaveDir);
    }

    public AuthenticationHeaderValue generateCredentials()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(plainPassword)) { throw new NotImplementedException(); }
        string authString = Username + ":" + plainPassword;
        string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
        return new AuthenticationHeaderValue("Basic", auth);
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
                    return decryptReader.ReadToEnd();
                }
            }
        }

    }
}

[SyncProviderID("Nextcloud")]
class NextcloudSyncProvider : SyncProvider
{
    private HttpClient nextcloudClient;

    private AuthenticationHeaderValue nextcloudCredentials;

    private Uri uri;

    private string prefix;

    public NextcloudSyncProvider()
    {
        initConfig();
        prefix = "remote.php/dav/files/" + Config.Instance.Nextcloud.Username + "/";
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

    private void initClient()
    {
        nextcloudClient = new HttpClient();
        nextcloudClient.BaseAddress = uri;
        nextcloudClient.DefaultRequestHeaders.Authorization = nextcloudCredentials;
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
        Config.Instance.Nextcloud.SaveDir = Console.ReadLine();
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
    private async Task<List<string>> Propfind(string path)
    {
        List<string> found = new List<string>();

        var method = new HttpMethod("PROPFIND");
        var req = new HttpRequestMessage(method, prefix + path);
        var res = await nextcloudClient.SendAsync(req);
        string xmlString = await res.Content.ReadAsStringAsync();

        XDocument xml = XDocument.Parse(xmlString);
        var paths = xml.Elements("{DAV:}multistatus").Elements("{DAV:}response").Elements("{DAV:}href");
        for (int i = 0; i < paths.Count(); i++)
        {
            string dir = paths.ElementAt(i).Value.TrimStart('/').Remove(0, prefix.Length);
            if (dir != prefix + path) found.Add(dir);
        }
        return found;
    }

    private async Task Mkcol(string path)
    {
        var method = new HttpMethod("MKCOL");
        var req = new HttpRequestMessage(method, prefix + path);
        var res = await nextcloudClient.SendAsync(req);
        if (res.StatusCode != HttpStatusCode.Created && res.StatusCode != HttpStatusCode.MethodNotAllowed)
        {
            throw new StatusCodeException(res.StatusCode);
        }
        return;
    }

    private async Task<byte[]> Get(string path)
    {
        var method = new HttpMethod("GET");
        var req = new HttpRequestMessage(method, prefix + path);
        var res = await nextcloudClient.SendAsync(req);
        if (res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsByteArrayAsync();
            return content;
        }
        else
        {
            return null;
        }
    }

    private async Task Put(string path, string file, bool isRaw = false)
    {
        var method = new HttpMethod("PUT");
        var req = new HttpRequestMessage(method, prefix + path);
        HttpResponseMessage res = new();
        if (isRaw)
        {
            var content = new StringContent(file);
            req.Content = content;
            res = await nextcloudClient.SendAsync(req);
        }
        else
        {
            var content = new StreamContent(File.OpenRead(file));
            req.Content = content;
            res = await nextcloudClient.SendAsync(req);
        }
        res.EnsureSuccessStatusCode();
        return;
    }

    private async Task Delete(string path)
    {
        var method = new HttpMethod("DELETE");
        var req = new HttpRequestMessage(method, prefix + path);
        var res = await nextcloudClient.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            throw new StatusCodeException(res.StatusCode);
        }
    }

    public void setSavePath(string path)
    {
        Config.Instance.Nextcloud.SaveDir = path;
    }

    private async Task<List<string>> getContents(string path)
    {
        List<string> contents = await Propfind(path);
        List<string> delete = new List<string>();
        contents.Remove(path);
        for (int i = 0; i < contents.Count; i++)
        {
            // removes otherwise duplicate / in query
            if (contents[i].EndsWith("/"))
            {
                var deeper = await getContents(contents[i]);
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

    private List<FileInfo> getFiles(DirectoryInfo d, Game game)
    {
        List<FileInfo> files = new();
        Regex include = new Regex(game.IncludeRegex);
        Regex exclude = new Regex(game.ExcludeRegex);
        foreach (FileInfo file in d.GetFiles())
        {
            if (exclude.IsMatch(file.Name))
            {
                if (include.IsMatch(file.Name))
                {
                    files.Add(file);
                }
            }
            else files.Add(file);
        }
        foreach (DirectoryInfo dir in d.GetDirectories())
        {
            if (exclude.IsMatch(dir.Name))
            {
                if (include.IsMatch(dir.Name))
                {
                    files.AddRange(getFiles(dir, game));
                }
            }
            else files.AddRange(getFiles(dir, game));
        }
        return files;
    }

    // SyncProvider Functions implemented here

    public override async Task DownloadFiles(Game game)
    {
        // check if saveDir exists
        DirectoryInfo saveDir = new(game.FullPath);
        if (!saveDir.Exists)
        {
            saveDir.Create();
        }
        List<string> files = await ListFiles(game);

        Regex includeRegex = new(game.IncludeRegex);
        Regex excludeRegex = new(game.ExcludeRegex);

        foreach (string file in files)
        {
            if (file != ".lastsync" && (!excludeRegex.IsMatch(file)) && includeRegex.IsMatch(file))
            {
                var content = await Get(Config.Instance.Nextcloud.SaveDir + "/" + game.Id + "/" + file);
                // check if subdirectory
                if (file.Contains('/'))
                {
                    DirectoryInfo fileDir = new(game.FullPath + "/" + file.Remove(file.LastIndexOf('/')));
                    if (!fileDir.Exists) fileDir.Create();
                }
                FileInfo fileHandle = new(game.FullPath + "/" + file);
                FileStream stream = fileHandle.OpenWrite();
                await stream.WriteAsync(content);
            }
        }
    }

    public override async Task<DateTime?> GetLastSyncTime(string gameId)
    {
        string path = Config.Instance.Nextcloud.SaveDir + "/" + gameId + "/.lastsync";
        var content = await Get(path);
        string data = Encoding.UTF8.GetString(content);
        var lastSyncMillis = Convert.ToInt64(data);
        var lastSync = new DateTime(lastSyncMillis);
        return lastSync;
    }

    public override async Task<SpaceUsage> GetSpaceUsage()
    {
        // easier to implement using OCS instead of WebDav
        // return free space from the quota
        string ocs = "ocs/v1.php/cloud/users/" + Config.Instance.Nextcloud.Username;

        string credentials = Config.Instance.Nextcloud.Username + ":" + Config.Instance.Nextcloud.plainPassword;
        var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));

        using var client = new HttpClient();

        client.BaseAddress = new Uri(Config.Instance.Nextcloud.Url);
        client.DefaultRequestHeaders.Add("OCS-APIRequest", "true");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

        var res = await client.GetAsync(ocs);
        string content = await res.Content.ReadAsStringAsync();

        List<XElement> quotaData = XElement.Parse(content).Elements("data").Elements("quota").ToList();

        SpaceUsage usage = new SpaceUsage();
        usage.TotalSpace = Convert.ToUInt64(quotaData.Elements("total").ToList()[0].Value);
        usage.FreeSpace = Convert.ToUInt64(quotaData.Elements("free").ToList()[0].Value);

        return usage;
    }

    public override async Task<List<string>> ListFiles(Game game)
    {
        List<string> contents = await getContents(Config.Instance.Nextcloud.SaveDir + "/" + game.Id + "/");
        for (int i = 0; i < contents.Count; i++)
        {
            contents[i] = contents[i].Remove(0, (Config.Instance.Nextcloud.SaveDir + "/" + game.Id + "/").Length);
        }
        return contents;
    }

    public override async Task UploadFiles(Game game, DateTime lastModTime)
    {
        string path = Config.Instance.Nextcloud.SaveDir + "/" + game.Id + "/";
        List<FileInfo> files = new();
        DirectoryInfo d = new DirectoryInfo(game.FullPath + "/");
        // recursive
        files.AddRange(getFiles(d, game));

        List<string> dirs = new();
        List<string> paths = new();
        foreach (FileInfo file in files)
        {
            string relDir = (file.DirectoryName + '/').Remove(0, (game.FullPath + '/').Length);
            string filePath = relDir + file.Name;
            if (!dirs.Contains(relDir) && relDir != "") dirs.Add(relDir);
            paths.Add(filePath);
        }
        // delete saveDir, doing this saves checking which files need to be deleted explicitly
        await Delete(path);
        // create saveDir and subDirs
        await Mkcol(path);
        foreach (string dir in dirs)
        {
            await Mkcol(path + dir);
        }

        foreach (string file in paths)
        {
            await Put(path + file, game.FullPath + '/' + file);
        }

        string now = lastModTime.Ticks.ToString();
        await Put(path + ".lastsync", now, true);
    }
}
