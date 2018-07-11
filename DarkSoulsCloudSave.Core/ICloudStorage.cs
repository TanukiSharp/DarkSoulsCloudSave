using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsCloudSave.Core
{
    /// <summary>
    /// Cloud storage file information.
    /// </summary>
    public struct CloudStorageFileInfo
    {
        /// <summary>
        /// Date and time format used to prefix filenames.
        /// </summary>
        public const string TimestampFormat = "yyyyMMddHHmmssfff";

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
        /// Gets an original remote file name (timestamped local filename).
        /// </summary>
        public string OriginalRemoteFilename { get; }

        /// <summary>
        /// Initializes a <see cref="CloudStorageFileInfo"/> instance.
        /// </summary>
        /// <param name="localFilename">The name of the file stored, or to be stored, on the local machine.</param>
        /// <param name="remoteFileIdentifier">The identifier that uniquely represent a file on the remote cloud.</param>
        /// 
        public static CloudStorageFileInfo ParseCloudStorageFileInfo(string localFilename, string remoteFileIdentifier)
        {
            if (localFilename.Length > TimestampFormat.Length + 1 && localFilename[TimestampFormat.Length] == '_')
            {
                if (DateTime.TryParseExact(localFilename.Substring(0, TimestampFormat.Length), TimestampFormat, null, DateTimeStyles.None, out DateTime dt))
                    return new CloudStorageFileInfo(dt, localFilename.Substring(TimestampFormat.Length + 1), localFilename, remoteFileIdentifier);
            }

            return new CloudStorageFileInfo(DateTime.MinValue, localFilename, localFilename, remoteFileIdentifier);
        }

        private CloudStorageFileInfo(DateTime storeTimestamp, string localFilename, string originalRemoteFilename, string remoteFileIdentifier)
        {
            StoreTimestamp = storeTimestamp;
            LocalFilename = localFilename;
            OriginalRemoteFilename = originalRemoteFilename;
            RemoteFileIdentifier = remoteFileIdentifier;
        }

        /// <summary>
        /// Returns a string representation of the current file information.
        /// </summary>
        /// <returns>Returns a string representation of the current file information.</returns>
        public override string ToString()
        {
            return $"{LocalFilename} ({RemoteFileIdentifier})";
        }
    }

    /// <summary>
    /// Represents an application-independent cloud storage.
    /// </summary>
    public interface ICloudStorage : IDisposable
    {
        /// <summary>
        /// Gets the display name of the current <see cref="ICloudStorage"/> instance.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes the cloud storage.
        /// </summary>
        /// <returns>Returns a task to be awaited until initialization is done.</returns>
        Task Initialize();

        /// <summary>
        /// Lists the available remote save data in the cloud storage.
        /// </summary>
        /// <returns>Returns an array of remote files.</returns>
        Task<IEnumerable<CloudStorageFileInfo>> ListFiles();

        /// <summary>
        /// Downloads a remote file to a local stream.
        /// </summary>
        /// <param name="fileInfo">The file information representing the remote file to download.</param>
        /// <returns>Returns a task to be awaited producing a stream containing the content of the remote file.</returns>
        Task<Stream> Download(CloudStorageFileInfo fileInfo);

        /// <summary>
        /// Uploads a local file to the cloud storage.
        /// </summary>
        /// <param name="localFilename">The name of the local file being uploaded.</param>
        /// <param name="stream">A readable stream containing the local file content to upload to the cloud storage.</param>
        /// <returns>Returns a task to be awaited until upload is done, true meaning success and false meaning a failure occured.</returns>
        Task<bool> Upload(string localFilename, Stream stream);

        /// <summary>
        /// Delete a remote file from the cloud storage.
        /// </summary>
        /// <param name="fileInfo">The file information representing the remote file to delete.</param>
        /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
        Task<bool> Delete(CloudStorageFileInfo fileInfo);

        /// <summary>
        /// Delete multiple remote files from the cloud storage.
        /// </summary>
        /// <param name="fileInfo">The file information representing the remote files to delete.</param>
        /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
        Task<bool> DeleteMany(IEnumerable<CloudStorageFileInfo> fileInfo);
    }
}
