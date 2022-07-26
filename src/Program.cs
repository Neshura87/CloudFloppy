using GameSync;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

class Program
{
	static DateTime GetLatestModifiedTime(string dir, Regex includeRegex, Regex excludeRegex)
	{
		var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
			.Where(f => includeRegex.IsMatch(f))
			.Where(f => !excludeRegex.IsMatch(f));

		if (files.Any())
			return files.Max(f => File.GetLastWriteTime(f));
		else return Directory.GetLastWriteTime(dir);
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
		[Value(0, HelpText = "ID of the game to run.")]
		public string GameID { get; set; } = null;

		[Option('b', "sync-before", HelpText = "Sync before running the game.")]
		public bool SyncBefore { get; set; } = true;
		[Option('a', "sync-after", HelpText = "Sync after the game exits.")]
		public bool SyncAfter { get; set; } = true;
		[Option('i', "interactive", HelpText = "User will be propmted when downloading and uploading saves.")]
		public bool Interactive { get; set; } = false;
	}

	[Verb("sync", HelpText = "Syncronize saves for games.")]
	class SyncOptions
	{
		[Value(0, Min = 1)]
		public IEnumerable<string> GameIDs { get; set; } = new string[] { };
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
		Config.LoadConfig();

		foreach (var game in Config.Instance.Games)
			await Sync(game, false);

		return await Task.FromResult(0);
	}

	static async Task<int> Sync(SyncOptions op)
	{
		Config.LoadConfig();

		foreach (string game in op.GameIDs.Distinct())
		{
			await Sync(Config.Instance.Games.FirstOrDefault(g => g.Id == game), false);
		}

		return await Task.FromResult(0);
	}

	static async Task Sync(Game game, bool interactive)
	{
		if (game == null) return;

		Console.WriteLine("Syncing " + game.Name);

		SyncProvider provider = SyncProvider.GetSyncProvider(Config.Instance.Provider);
		DateTime? lastSyncTime = await provider.GetLastSyncTime(game.Id);

		if (!Directory.Exists(game.FullPath))
		{
			Console.WriteLine("Game directory does not exist, downloading...");
			if (lastSyncTime == null)
			{
				Console.WriteLine("Game not found on server, skipping");
				return;
			}

			await provider.DownloadFiles(game);
			return;
		}

		DateTime localModifiedTime = GetLatestModifiedTime(game.FullPath + "/",
			 new Regex(game.IncludeRegex), new Regex(game.ExcludeRegex));
       
		if (lastSyncTime == null)
		{
			Console.WriteLine("Game " + game.Name + " not on remote server, uploading...");
			await provider.UploadFiles(game, localModifiedTime);
			return;
		}

		var t = lastSyncTime ?? new DateTime(0);
		if (t.Ticks > localModifiedTime.Ticks)
		{
			Console.WriteLine("Local save out of date, syncing...");
			await provider.DownloadFiles(game);
		}
		else if (t.Ticks < localModifiedTime.Ticks)
		{
			Console.WriteLine("Remote save out of date, syncing...");
			await provider.UploadFiles(game, localModifiedTime);
		}

		Console.WriteLine("Saves up to date!");
	}

	static async Task<int> List(ListOptions op)
	{
		Config.LoadConfig();

		SyncProvider provider = SyncProvider.GetSyncProvider(Config.Instance.Provider);

		SpaceUsage su = await provider.GetSpaceUsage();

		Console.WriteLine("Free Space: " + su.humanReadable(su.FreeSpace));

		Console.WriteLine("Available games:");
		foreach (var game in Config.Instance.Games)
			Console.WriteLine($"{game.Name} ({game.Id})");

		return await Task.FromResult(0);
	}

	static async Task<int> Run(RunOptions op)
	{
		Config.LoadConfig();
		var game = Config.Instance.Games.FirstOrDefault(g => g.Id == op.GameID);
		if (game == null)
		{
			Console.WriteLine("Game not found.");
			return await Task.FromResult(1);
		}

		if (op.SyncBefore)
			await Sync(game, op.Interactive);

		Console.WriteLine("Running " + game.Name);

		var psi = new ProcessStartInfo("sh");
		psi.ArgumentList.Add("-c");
		psi.WorkingDirectory = game.GameDirectory;
		psi.ArgumentList.Add(game.ShellCommand);
		var proc = new Process();
		proc.StartInfo = psi;
		proc.Start();
		await proc.WaitForExitAsync();

		if (op.SyncAfter)
			await Sync(game, op.Interactive);

		return await Task.FromResult(proc.ExitCode);
	}
}
