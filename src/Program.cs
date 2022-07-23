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

SyncProvider provider = null;
string gameId = "my-game";
string saveDir = "saves";
Regex includeRegex = new Regex("*");
Regex excludeRegex = new Regex("screenshots", RegexOptions.IgnoreCase);

// Sync saves to local
{
	DateTime? lastSyncTime = await provider.GetLastSyncTime(gameId);
	if (lastSyncTime == null)
		Console.WriteLine("Game " + gameId + " not on remote server");

	DateTime localModifiedTime = GetLatestModifiedTime(saveDir, includeRegex, excludeRegex);

	if (lastSyncTime > localModifiedTime)
	{
		Console.WriteLine("Local save out of date, syncing...");
		await provider.DownloadFiles(gameId, saveDir);
		Console.WriteLine("Saves synced!");
	}
}

// Launch game and wait for exit

// Sync saves to remote
{
	DateTime? lastSyncTime = await provider.GetLastSyncTime(gameId);
	if (lastSyncTime == null)
		Console.WriteLine("Game " + gameId + " not on remote server");

	DateTime localModifiedTime = GetLatestModifiedTime(saveDir, includeRegex, excludeRegex);

	if (lastSyncTime < localModifiedTime)
	{
		Console.WriteLine("Remote save out of date, syncing...");
		await provider.UploadFiles(gameId, saveDir);
		Console.WriteLine("Saves synced!");
	}
}
