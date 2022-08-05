using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

namespace CloudFloppy;

public class SpaceUsage
{
	/// <summary>
	/// Total available space in bytes.
	/// </summary>
	public ulong TotalSpace;
	/// <summary>
	/// Available space in bytes.
	/// </summary>
	public ulong FreeSpace;

	public string humanReadable(ulong input)
	{
		string size;
		const double kb = 1024;
		const double mb = 1024*1024;
		const double gb = mb*1024;
		switch(input)
		{
			case >= (ulong)gb:
				size = Math.Round((input/gb), 2).ToString() + " GB";
				break;
			case >= (ulong)mb:
				size = Math.Round((input/mb), 2).ToString() + " MB";
				break;
			case >= (ulong)kb:
				size = Math.Round((input/kb), 2).ToString() + " KB";
				break;
			case < 1024:
				size = input.ToString() + " Bytes";
				break;
		}
		return size;
	}
}

[AttributeUsage(AttributeTargets.Class)]
public class SyncProviderIDAttribute : Attribute
{
	public string Id;
	public SyncProviderIDAttribute(string id)
	{
		Id = id;
	}
}

public abstract class SyncProvider
{
	public static SyncProvider GetSyncProvider(string id)
	{
		var t = typeof(SyncProvider).Assembly.GetTypes()
			.Where(t => t.IsAssignableTo(typeof(SyncProvider)))
			.FirstOrDefault(t => t.GetCustomAttribute<SyncProviderIDAttribute>(false)?.Id == id);

		if (t == null)
			throw new Exception("Sync provider not found: " + id);

		return (SyncProvider)Activator.CreateInstance(t);
	}

	/// <summary>
	/// Get the last time saves for a game were synced.
	/// </summary>
	/// <param name="gameId">The ID of the game to check</param>
	/// <returns>Last sync time or null if never synced</returns>
	public abstract Task<DateTime?> GetLastSyncTime(string gameId);
	
	/// <summary>
	/// Return a list of files stored for a game.
	/// </summary>
	/// <param name="gameId">The game to list files for</param>
	/// <returns>List of relative file paths</returns>
	public abstract Task<List<string>> ListFiles(Game game);

	/// <summary>
	/// Downloads all the remote files to a local directory.
	/// </summary>
	/// <param name="gameId">The game to download files for</param>
	public abstract Task DownloadFiles(Game game);

	/// <summary>
	/// Upload files from local directory to remote server.
	/// </summary>
	/// <param name="gameId">The game to upload files for</param>
	/// <param name="lastModTime">The time the last file was modified at</param>
	/// <returns></returns>
	public abstract Task UploadFiles(Game game, DateTime lastModTime);

	/// <summary>
	/// Returns the total and available space on the remote server.
	/// </summary>
	/// <returns>Total and available space on the remote server</returns>
	public abstract Task<SpaceUsage> GetSpaceUsage();
}