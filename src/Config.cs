namespace GameSync
{
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
            password = "";
            username = "";
            url = "";
        }
        public string password { get; set; }
        public string username { get; set; }
        public string url { get; set; }

        public bool IsNullOrEmpty()
        {
            if (hasCredentials() && hasUrl())
            { return false; }
            else { return true; }
        }

        public bool hasCredentials()
        {
            if (string.IsNullOrEmpty(password) && string.IsNullOrEmpty(username))
            { return false; }
            else { return true; }
        }
        public bool hasUrl()
        {
            return !string.IsNullOrEmpty(url);
        }
    }
}