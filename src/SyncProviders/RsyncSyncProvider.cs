using System.Diagnostics;

namespace GameSync;

public class RSyncSyncProvider : SyncProvider
{
	public string? username;
	public string? host;
	/// <summary>
	/// The remote directory, which contains the game files.
	/// </summary>
	public string saveDir = "Saves";

	const string lastSyncFile = ".lastsync";

	/// <summary>
	/// Returns `user@host` or `host` if username is empty.
	/// </summary>
	string? getSshString()
		=> string.IsNullOrEmpty(username) ?
			host :
			$"{username}@{host}";

	public override async Task DownloadFiles(string gameId, string outDir)
	{
		var startinfo = new ProcessStartInfo();
		startinfo.FileName = "rsync";
		// recursive
		startinfo.ArgumentList.Add("-rtN");
		startinfo.ArgumentList.Add(getSshString() + ":" + saveDir + "/" + gameId + "/");
		startinfo.ArgumentList.Add(outDir + "/");
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
		startinfo.ArgumentList.Add(saveDir + "/" + gameId + "/" + lastSyncFile);
		startinfo.RedirectStandardError = true;
		startinfo.RedirectStandardOutput = true;

		var proc = new Process();
		proc.StartInfo = startinfo;
		proc.Start();

		await proc.WaitForExitAsync();

		if (proc.ExitCode != 0)
			return null;

		string str = proc.StandardOutput.ReadToEnd().Trim();
		return new DateTime(
			long.Parse(str));
	}

	public override async Task<SpaceUssage> GetSpaceUsage()
	{
		var startinfo = new ProcessStartInfo();
		startinfo.FileName = "ssh";
		startinfo.ArgumentList.Add(getSshString());
		startinfo.ArgumentList.Add("df");
		startinfo.ArgumentList.Add("--block-size=1");
		startinfo.ArgumentList.Add("-P");
		startinfo.ArgumentList.Add(saveDir);
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
		var su = new SpaceUssage();
		su.TotalSpace = ulong.Parse(cols[1]);
		su.FreeSpace = ulong.Parse(cols[3]);

		return su;
	}

	public override async Task<List<string>> ListFiles(string gameId)
	{
		string dir = saveDir + "/" + gameId;

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

	public override async Task UploadFiles(string gameId, string inDir, DateTime lastModTime)
	{
		{
			var startinfo = new ProcessStartInfo();
			startinfo.FileName = "rsync";
			// recursive
			startinfo.ArgumentList.Add("-rtN");
			startinfo.ArgumentList.Add(inDir + "/");
			startinfo.ArgumentList.Add(getSshString() + ":" + saveDir + "/" + gameId + "/");
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
			startinfo.ArgumentList.Add(saveDir + "/" + gameId + "/" + lastSyncFile);
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