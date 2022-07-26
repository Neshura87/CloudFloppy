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
		public string GameID { get; set; } = null;
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

		foreach(var game in Config.Instance.Games)
			await Sync(game);

		return await Task.FromResult(0);
	}

	static async Task<int> Sync(SyncOptions op)
	{
		Config.LoadConfig();

		foreach (string game in op.GameIDs.Distinct())
		{
			await Sync(Config.Instance.Games.FirstOrDefault(g => g.Id == game));
		}

		return await Task.FromResult(0);
	}

	static async Task Sync(Game g)
	{
		if(g == null) return;

		Console.WriteLine("Syncing " + g.Name);		
	}

	static async Task<int> List(ListOptions op)
	{
		Config.LoadConfig();

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

		return 0;
	}

}
