using GameSync;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

class Program
{
	static DateTime GetLatestModifiedTime(string dir, Regex includeRegex, Regex excludeRegex)
	{
		return Directory.GetFiles(dir, ".*", SearchOption.AllDirectories)
			.Where(f => !excludeRegex.IsMatch(f))
			.Where(f => includeRegex.IsMatch(f))
			.Max(f => File.GetLastAccessTime(f));
	}

	static async Task<int> Main(string[] args)
	{
		var parser = new Parser(p => { p.AutoHelp = true; });
		var pargs = parser.ParseArguments<ListOptions, RunOptions, SyncOptions, SyncAllOptions>(args);
		return await pargs.MapResult
			(
				(ListOptions op) => List(op),
				(RunOptions op) => Run(op),
				(SyncAllOptions op) => SyncAll(op),
				(SyncOptions op) => Sync(op),
				(errs) => { Console.Write(HelpText.AutoBuild(pargs)); return Task.FromResult(1); }
			);
	}

	[Verb("run", HelpText = "Run and sync the game.")]
	class RunOptions
	{
		[Value(0)]
		public string? GameID { get; set; } = null;
	}

	[Verb("sync", HelpText = "Syncronize saves for games.")]
	class SyncOptions
	{
		[Value(0, Min = 1)]
		public IEnumerable<string?> GameIDs { get; set; } = new string?[]{};
	}

	[Verb("syncall", HelpText = "Syncronize saves for all games.")]
	class SyncAllOptions
	{

	}

	[Verb("list", HelpText = "List all games.")]
	class ListOptions
	{

	}

	static async Task<int> SyncAll(SyncAllOptions op)
	{
		return await Sync(new SyncOptions
		{
			GameIDs = new string[] { "game1", "game2", "game3"
			}
		});
	}

	static async Task<int> Sync(SyncOptions op)
	{
		foreach (string game in op.GameIDs.Distinct())
			Console.WriteLine("Syncing " + game);

		return await Task.FromResult(0);
	}

	static async Task<int> List(ListOptions op)
	{
		Console.WriteLine("Available games:");
		foreach (string game in new string[] { "game1", "game2", "game3" })
			Console.WriteLine(game);

		return await Task.FromResult(0);
	}

	static async Task<int> Run(RunOptions op)
	{
		Console.WriteLine("Running " + op.GameID);

		return await Task.FromResult(0);
	}

	static async Task<int> Demo()
	{
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

		return 0;
	}

}
