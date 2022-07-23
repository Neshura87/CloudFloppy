using GameSync;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

DateTime GetLatestModifiedTime(string dir, Regex includeRegex, Regex excludeRegex)
{
	return Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
		.Where(f => !excludeRegex.IsMatch(f))
		.Where(f => includeRegex.IsMatch(f))
		.Max(f => File.GetLastAccessTime(f));
}

SyncProvider provider = new RSyncSyncProvider();
var rs = (RSyncSyncProvider)provider;
rs.host = "mc.ryhn.link";
rs.username = "ubuntu";

string gameId = "holocure";
string saveDir = "/home/ryhon/.wine/drive_c/users/ryhon/AppData/Local/HoloCure";
Regex includeRegex = new Regex(".*");
Regex excludeRegex = new Regex("screenshots", RegexOptions.IgnoreCase);

var space = await provider.GetSpaceUsage();
Console.WriteLine("Available space: " + space.FreeSpace + " bytes");
Console.WriteLine("Total space: " + space.TotalSpace + " bytes");
Console.WriteLine("Used precentage: " + ((space.TotalSpace - space.FreeSpace) / (float)space.TotalSpace) * 100 + "% used");

// Sync saves to local
{
	DateTime? lastSyncTime = await provider.GetLastSyncTime(gameId);
	if (lastSyncTime == null)
		Console.WriteLine("Game " + gameId + " not on remote server");
	else
	{
		Console.WriteLine("Remote files:");
		foreach (string f in await provider.ListFiles(gameId))
			Console.WriteLine("\t" + f);

		DateTime localModifiedTime = GetLatestModifiedTime(saveDir, includeRegex, excludeRegex);

		if ((lastSyncTime ?? new DateTime(0)) > localModifiedTime)
		{
			Console.WriteLine("Local save out of date, syncing...");
			await provider.DownloadFiles(gameId, saveDir);
			Console.WriteLine("Saves synced!");
		}
		else Console.WriteLine("Saves up to date!");
	}
}

// Launch game and wait for exit
Console.WriteLine("Playing game...");
Console.WriteLine("Press any key to continue...");
Console.ReadLine();

// Sync saves to remote
{
	DateTime? lastSyncTime = await provider.GetLastSyncTime(gameId);
	if (lastSyncTime == null)
		Console.WriteLine("Game " + gameId + " not on remote server");

	DateTime localModifiedTime = GetLatestModifiedTime(saveDir, includeRegex, excludeRegex);

	if ((lastSyncTime ?? new DateTime(0)) < localModifiedTime)
	{
		Console.WriteLine("Remote save out of date, syncing...");
		await provider.UploadFiles(gameId, saveDir, localModifiedTime);
		Console.WriteLine("Saves synced!");
	}
	else Console.WriteLine("Saves up to date!");
}
