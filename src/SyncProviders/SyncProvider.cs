using System;
using System.Threading;
using System.Collections.Generic;

namespace GameSync;

public class SpaceUssage
{
	/// <summary>
	/// Total available space in bytes.
	/// </summary>
	public ulong TotalSpace;
	/// <summary>
	/// Available space in bytes.
	/// </summary>
	public ulong FreeSpace;
}

public abstract class SyncProvider
{
	/// <summary>
	/// Get the last time saves for a game were synced.
	/// </summary>
	/// <param name="gameId">The ID of the game to check</param>
	/// <returns>Last sync time or null if never synced</returns>
	public abstract Task<DateTime?> GetLastSyncTime(string gameId);
	
	/// <summary>
	/// Return a list of files stored for a game.
	/// </summary>
	/// <param name="gameId">The ID of the game to check</param>
	/// <returns>IEnumerable of relative file paths</returns>
	public abstract Task<List<string>> ListFiles(string gameId);

	/// <summary>
	/// Downloads all the remote files to a local directory.
	/// </summary>
	/// <param name="gameId">The ID of the game to download files for</param>
	/// <param name="outDir">The output directory</param>
	public abstract Task DownloadFiles(string gameId, string outDir);

	/// <summary>
	/// Upload files from local directory to remote server.
	/// </summary>
	/// <param name="gameId">The ID of the game to upload files for</param>
	/// <param name="inDir">The local directory containing save data</param>
	/// <param name="lastModTime">The time the last file was modified at</param>
	/// <returns></returns>
	public abstract Task UploadFiles(string gameId, string inDir, DateTime lastModTime);

	/// <summary>
	/// Returns the total and available space on the remote server.
	/// </summary>
	/// <returns>Total and available space on the remote server</returns>
	public abstract Task<SpaceUssage> GetSpaceUsage();
}