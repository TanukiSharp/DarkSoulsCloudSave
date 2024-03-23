using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamCloudSave.Core;

/// <summary>
/// Defines what is archived.
/// </summary>
public enum ArchiveMode
{
    /// <summary>
    /// The whole root folder is archived.
    /// </summary>
    WholeFolder,
    /// <summary>
    /// All sub-folders of first level only are archived independently from each others, files are ignored.
    /// </summary>
    SubFolders,
}

/// <summary>
/// Provides helper methods related to game save data management.
/// </summary>
public class SaveDataUtility
{
    private readonly ArchiveMode archiveMode;

    /// <summary>
    /// Initializes the <see cref="SaveDataUtility"/> instance.
    /// </summary>
    /// <param name="saveDataPath">Full path where save data is located.</param>
    /// <param name="archiveMode">Tells what is archived.</param>
    public SaveDataUtility(string saveDataPath, ArchiveMode archiveMode)
    {
        ArgumentNullException.ThrowIfNull(saveDataPath);

        this.archiveMode = archiveMode;

        saveDataPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(saveDataPath));

        string lastPathPart = Path.GetFileName(saveDataPath);

        SaveDataPath = saveDataPath;

        BackupsPath = Path.GetFullPath($"./backups/{lastPathPart}");
        if (Directory.Exists(BackupsPath) == false)
            Directory.CreateDirectory(BackupsPath);
    }


    /// <summary>
    /// Gets the full path where save data files are located.
    /// This is outside the application folder.
    /// </summary>
    public string SaveDataPath { get; private set; }

    /// <summary>
    /// Gets the fill path where save data backups are stored.
    /// This is inside the application folder.
    /// </summary>
    public string BackupsPath { get; private set; }

    /// <summary>
    /// Gets a stream containing the compressed content of the given folder.
    /// </summary>
    /// <returns>Returns a stream containing the compressed content of the given folder.</returns>
    public async Task<Stream> GetSaveDataArchive()
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            IEnumerable<string> files;

            if (archiveMode == ArchiveMode.WholeFolder)
            {
                files = Directory.EnumerateFiles(SaveDataPath, "*.*", SearchOption.AllDirectories);
                await AddFileToZipArchive(archive, files);
            }
            else if (archiveMode == ArchiveMode.SubFolders)
            {
                IEnumerable<string> dirs = Directory.EnumerateDirectories(SaveDataPath, "*", SearchOption.TopDirectoryOnly);
                foreach (string dir in dirs)
                {
                    files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories);
                    await AddFileToZipArchive(archive, files);
                }
            }
        }

        stream.Position = 0;

        return stream;
    }

    private async Task AddFileToZipArchive(ZipArchive archive, IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            string filename = file.Substring(SaveDataPath.Length + 1, file.Length - SaveDataPath.Length - 1);
            ZipArchiveEntry entry = archive.CreateEntry(filename, CompressionLevel.Optimal);
            using var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using Stream targetStream = entry.Open();
            await sourceStream.CopyToAsync(targetStream);
        }
    }

    /// <summary>
    /// Uncompress a stream and extracts contained files on the local storage.
    /// </summary>
    /// <param name="archiveStream">A stream containing the compressed files.</param>
    /// <returns>Returns a task to be awaited until the extraction process is done.</returns>
    public async Task ExtractSaveDataArchive(Stream archiveStream)
    {
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, false, Encoding.UTF8);

        foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.Length > 0))
        {
            string targetFilename = Path.Combine(SaveDataPath, entry.FullName);
            string targetDirectory = Path.GetDirectoryName(targetFilename);
            Directory.CreateDirectory(targetDirectory);

            using Stream sourceStream = entry.Open();
            using var targetStream = new FileStream(targetFilename, FileMode.OpenOrCreate, FileAccess.Write);

            await sourceStream.CopyToAsync(targetStream);
        }
    }

    /// <summary>
    /// Backs up all the local save data to the backup folder, in a compressed form.
    /// </summary>
    /// <returns>Returns a task to be awaited until the backup process is done.</returns>
    public async Task BackupLocalSaveData()
    {
        if (Directory.Exists(SaveDataPath) == false)
            return;

        string now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");

        string filename = $"{now}.zip";

        Stream archiveStream = await GetSaveDataArchive();
        using var targetStream = new FileStream(Path.Combine(BackupsPath, filename), FileMode.OpenOrCreate, FileAccess.Write);

        await archiveStream.CopyToAsync(targetStream);
    }
}
