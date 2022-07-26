using System.Diagnostics;

namespace GameSync;

public class RsyncConfig
{
	public string Username { get; set; }
	public string Host { get; set; }
	/// <summary>
	/// The remote directory, which contains the game files.
	/// </summary>
	public string SaveDir { get; set; } = "Saves";
}

[SyncProviderID("RSync")]
public class RSyncSyncProvider : SyncProvider
{
	const string lastSyncFile = ".lastsync";

	/// <summary>
	/// Returns `user@host` or `host` if username is empty.
	/// </summary>
	string getSshString()
		=> string.IsNullOrEmpty(Config.Instance.Rsync.Username) ?
			Config.Instance.Rsync.Host :
			$"{Config.Instance.Rsync.Username}@{Config.Instance.Rsync.Host}";

	async Task mkdir(string dir)
	{
				var startinfo = new ProcessStartInfo();
		startinfo.FileName = "ssh";
		// recursive
		startinfo.ArgumentList.Add(getSshString());
		startinfo.ArgumentList.Add("mkdir");
		startinfo.ArgumentList.Add("-p");
		startinfo.ArgumentList.Add(dir);
		startinfo.RedirectStandardError = true;
		startinfo.RedirectStandardOutput = true;

		var proc = new Process();
		proc.StartInfo = startinfo;
		proc.Start();

		await proc.WaitForExitAsync();
	}

	public override async Task DownloadFiles(Game game)
	{
		await mkdir(Config.Instance.Rsync.SaveDir + "/" + game.Id + "/");

		var startinfo = new ProcessStartInfo();
		startinfo.FileName = "rsync";
		// recursive
		startinfo.ArgumentList.Add("-rt");
		startinfo.ArgumentList.Add(getSshString() + ":" + Config.Instance.Rsync.SaveDir + "/" + game.Id + "/");
		startinfo.ArgumentList.Add(game.FullPath + "/");
		startinfo.ArgumentList.Add("--include");
		startinfo.ArgumentList.Add(game.IncludeRegex);
		startinfo.ArgumentList.Add("--exclude");
		startinfo.ArgumentList.Add(game.ExcludeRegex);
		startinfo.ArgumentList.Add("--exclude");
		startinfo.ArgumentList.Add(lastSyncFile);
		startinfo.RedirectStandardError = true;
		startinfo.RedirectStandardOutput = true;

		var proc = new Process();
		proc.StartInfo = startinfo;
		proc.Start();

		await proc.WaitForExitAsync();

		if (proc.ExitCode != 0)
			throw new Exception("rsync returned with a non-zero code\n"
			+ proc.StandardOutput.ReadToEnd()
			+ proc.StandardError.ReadToEnd());
	}

	public override async Task<DateTime?> GetLastSyncTime(string gameId)
	{
		var startinfo = new ProcessStartInfo();
		startinfo.FileName = "ssh";
		// recursive
		startinfo.ArgumentList.Add(getSshString());
		startinfo.ArgumentList.Add("cat");
		startinfo.ArgumentList.Add(Config.Instance.Rsync.SaveDir + "/" + gameId + "/" + lastSyncFile);
		startinfo.RedirectStandardError = true;
		startinfo.RedirectStandardOutput = true;

		var proc = new Process();
		proc.StartInfo = startinfo;
		proc.Start();

		await proc.WaitForExitAsync();

		if (proc.ExitCode != 0)
			return null;


		string str = proc.StandardOutput.ReadToEnd().Trim();
		return new DateTime(long.Parse(str));
	}

	public override async Task<SpaceUsage> GetSpaceUsage()
	{
		var startinfo = new ProcessStartInfo();
		startinfo.FileName = "ssh";
		startinfo.ArgumentList.Add(getSshString());
		startinfo.ArgumentList.Add("df");
		startinfo.ArgumentList.Add("--block-size=1");
		startinfo.ArgumentList.Add("-P");
		startinfo.ArgumentList.Add(Config.Instance.Rsync.SaveDir);
		startinfo.RedirectStandardError = true;
		startinfo.RedirectStandardOutput = true;

		var proc = new Process();
		proc.StartInfo = startinfo;
		proc.Start();

		await proc.WaitForExitAsync();

		if (proc.ExitCode != 0)
			throw new Exception("ssh returned with a non-zero code:\n"
			+ proc.StandardOutput.ReadToEnd()
			+ proc.StandardError.ReadToEnd());

		string[] cols = proc.StandardOutput.ReadToEnd().Split('\n')[1].Split(' ').Where(s => s != "").ToArray();
		var su = new SpaceUsage();
		su.TotalSpace = ulong.Parse(cols[1]);
		su.FreeSpace = ulong.Parse(cols[3]);

		return su;
	}

	public override async Task<List<string>> ListFiles(Game game)
	{
		string dir = Config.Instance.Rsync.SaveDir + "/" + game.Id;

		var startinfo = new ProcessStartInfo();
		startinfo.FileName = "ssh";
		startinfo.ArgumentList.Add(getSshString());
		startinfo.ArgumentList.Add("find");
		startinfo.ArgumentList.Add(dir);
		startinfo.RedirectStandardError = true;
		startinfo.RedirectStandardOutput = true;

		var proc = new Process();
		proc.StartInfo = startinfo;
		proc.Start();

		await proc.WaitForExitAsync();

		if (proc.ExitCode != 0)
			throw new Exception("ssh returned with a non-zero code\n"
			+ proc.StandardOutput.ReadToEnd()
			+ proc.StandardError.ReadToEnd());

		return proc.StandardOutput.ReadToEnd()
			.Split('\n')
			.Where(f => f != dir)
			.Where(f => !string.IsNullOrWhiteSpace(f))
			.Where(f => f != lastSyncFile)
			.Select(f => f[(dir.Length + 1)..])
			.ToList();
	}

	public override async Task UploadFiles(Game game, DateTime lastModTime)
	{
		await mkdir(Config.Instance.Rsync.SaveDir + "/" + game.Id + "/");

		{
			var startinfo = new ProcessStartInfo();
			startinfo.FileName = "rsync";
			// recursive
			startinfo.ArgumentList.Add("-rt");
			startinfo.ArgumentList.Add(game.FullPath + "/");
			startinfo.ArgumentList.Add(getSshString() + ":" + Config.Instance.Rsync.SaveDir + "/" + game.Id + "/");
			startinfo.ArgumentList.Add("--include");
			startinfo.ArgumentList.Add(game.IncludeRegex);
			startinfo.ArgumentList.Add("--exclude");
			startinfo.ArgumentList.Add(game.ExcludeRegex);
			startinfo.RedirectStandardError = true;
			startinfo.RedirectStandardOutput = true;

			var proc = new Process();
			proc.StartInfo = startinfo;
			proc.Start();

			await proc.WaitForExitAsync();

			if (proc.ExitCode != 0)
				throw new Exception("rsync returned with a non-zero code\n"
				+ proc.StandardOutput.ReadToEnd()
				+ proc.StandardError.ReadToEnd());
		}

		{
			var startinfo = new ProcessStartInfo();
			startinfo.FileName = "ssh";
			startinfo.ArgumentList.Add(getSshString());
			startinfo.ArgumentList.Add("echo");
			startinfo.ArgumentList.Add(lastModTime.Ticks.ToString());
			startinfo.ArgumentList.Add(">");
			startinfo.ArgumentList.Add(Config.Instance.Rsync.SaveDir + "/" + game.Id + "/" + lastSyncFile);
			startinfo.RedirectStandardError = true;
			startinfo.RedirectStandardOutput = true;

			var proc = new Process();
			proc.StartInfo = startinfo;
			proc.Start();

			await proc.WaitForExitAsync();

			if (proc.ExitCode != 0)
				throw new Exception("ssh returned with a non-zero code\n"
				+ proc.StandardOutput.ReadToEnd()
				+ proc.StandardError.ReadToEnd());
		}
	}
}