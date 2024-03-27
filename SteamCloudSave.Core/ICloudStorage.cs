using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SteamCloudSave.Core;

/// <summary>
/// Cloud storage file information.
/// </summary>
public struct CloudStorageFileInfo
{
    /// <summary>
    /// Date and time format used to prefix filenames.
    /// </summary>
    public const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss-fff";

    /// <summary>
    /// Gets the date and time when the file was stored to the cloud.
    /// </summary>
    public DateTime StoreTimestamp { get; }

    /// <summary>
    /// Gets the name of the file stored, or to be stored, on the local machine.
    /// </summary>
    public string LocalFilename { get; }

    /// <summary>
    /// Gets an identifier that uniquely represent a file on the remote cloud.
    /// </summary>
    public string RemoteFileIdentifier { get; }

    /// <summary>
    /// Initializes a <see cref="CloudStorageFileInfo"/> instance.
    /// </summary>
    /// <param name="localFilename">The name of the file stored, or to be stored, on the local machine.</param>
    /// <param name="remoteFileIdentifier">The identifier that uniquely represent a file on the remote cloud.</param>
    /// <returns>Returns a <see cref="CloudStorageFileInfo"/> instance.</returns>
    public static CloudStorageFileInfo ParseCloudStorageFileInfo(string localFilename, string remoteFileIdentifier)
    {
        string localFilenameWithoutExtension = Path.GetFileNameWithoutExtension(localFilename);

        if (localFilenameWithoutExtension.Length == TimestampFormat.Length)
        {
            if (DateTime.TryParseExact(localFilenameWithoutExtension, TimestampFormat, null, DateTimeStyles.None, out DateTime dt))
                return new CloudStorageFileInfo(dt, localFilename, remoteFileIdentifier);
        }

        return new CloudStorageFileInfo(DateTime.MinValue, localFilename, remoteFileIdentifier);
    }

    private CloudStorageFileInfo(DateTime storeTimestamp, string localFilename, string remoteFileIdentifier)
    {
        StoreTimestamp = storeTimestamp;
        LocalFilename = localFilename;
        RemoteFileIdentifier = remoteFileIdentifier;
    }

    /// <summary>
    /// Returns a string representation of the current file information.
    /// </summary>
    /// <returns>Returns a string representation of the current file information.</returns>
    public override readonly string ToString()
    {
        return $"{LocalFilename} ({RemoteFileIdentifier})";
    }
}

/// <summary>
/// Represents an application-independent cloud storage.
/// </summary>
public interface ICloudStorage : IAsyncDisposable
{
    /// <summary>
    /// Gets the display name of the current <see cref="ICloudStorage"/> instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Initializes the cloud storage.
    /// </summary>
    /// <returns>Returns a task to be awaited until initialization is done.</returns>
    Task InitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lists the available remote save data in the cloud storage.
    /// </summary>
    /// <param name="path">The path where to list files.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns an array of remote files.</returns>
    Task<IReadOnlyList<CloudStorageFileInfo>> ListFilesAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a remote file to a local stream.
    /// </summary>
    /// <param name="fileInfo">The file information representing the remote file to download.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited producing a stream containing the content of the remote file.</returns>
    Task<Stream> DownloadAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a local file to the cloud storage.
    /// </summary>
    /// <param name="remoteFilename">The full filename to be given to the remote file.</param>
    /// <param name="stream">A readable stream containing the local file content to upload to the cloud storage.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until upload is done, true meaning success and false meaning a failure occured.</returns>
    Task<bool> UploadAsync(string remoteFilename, Stream stream, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a remote file from the cloud storage.
    /// </summary>
    /// <param name="fileInfo">The file information representing the remote file to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
    Task<bool> DeleteAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Delete multiple remote files from the cloud storage.
    /// </summary>
    /// <param name="fileInfo">The file information representing the remote files to delete.</param>
    /// <param name="perFileTimeout">Time allowed per file deletion.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
    Task<bool> DeleteManyAsync(IEnumerable<CloudStorageFileInfo> fileInfo, TimeSpan perFileTimeout, CancellationToken cancellationToken);
}
