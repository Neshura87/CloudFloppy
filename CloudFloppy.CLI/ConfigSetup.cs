using CloudFloppy;

class ConfigSetup
{
    static public void CreateConfig()
    {
        Config NewConfig = new();
        Console.WriteLine("No Config file was found, do you want to create one? [Y]/[N]");
        string ans = Console.ReadLine();
        if (ans != "Y" && ans != "y")
        {
            Console.WriteLine("Exiting Application");
            Environment.Exit(0);
        }
        NewConfig.Provider = ChooseSyncProvider();
       

        Config.LoadConfig();
    }

    static private string ChooseSyncProvider(bool invalid=false)
    {
        Console.Clear();
        if(invalid) Console.WriteLine("-- Invalid input, please try again --");
        Console.WriteLine("Please choose one of the following Sync-Providers:");
        Console.WriteLine("[1] Nextcloud\n"
                        + "[2] RSync");
        string sel = Console.ReadLine();
        switch (sel){
            case "1":
                return "Nextcloud";
            case "2":
                return "RSync";
            default:
                return ChooseSyncProvider(true);
        }
    }
}