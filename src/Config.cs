using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace GameSync;

public class Config
{
    public Config()
    {
        nextcloud = new NexctloudConfig();
    }
    public NexctloudConfig nextcloud { get; set; }

    public bool IsNullOrEmpty()
    {
        if (nextcloud.IsNullOrEmpty())
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}

public class NexctloudConfig
{
    public NexctloudConfig()
    {
        _password = new byte[0];
        username = "";
        url = "";
        path = "";
    }
    // key generated using random numbers
    private static byte[] key =
                {
                    0x65, 0xcd, 0xdd, 0x5d, 0x56, 0xd6, 0xc7, 0x22,
                    0xbd, 0xdf, 0xd4, 0xa6, 0x6f, 0x1e, 0xe1, 0xdd
                    };

    [JsonIgnore]
    private byte[] _password;

    public byte[] password
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
    public string username { get; set; }
    public string url { get; set; }

    public string path { get; set; }

    public bool IsNullOrEmpty()
    {
        if (hasCredentials() || hasUrl())
        { return false; }
        else { return true; }
    }

    public bool hasCredentials()
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(username))
        { return false; }
        else { return true; }
    }

    public bool hasUrl()
    {
        return !string.IsNullOrEmpty(url);
    }

    public NetworkCredential generateCredentials()
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(plainPassword)) { }
        NetworkCredential auth = new NetworkCredential(username, plainPassword);
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