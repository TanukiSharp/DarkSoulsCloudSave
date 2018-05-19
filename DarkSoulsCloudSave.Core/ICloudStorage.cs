using System;
using System.Collections.Generic;
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
        /// Gets the name of the file stored, or to be stored, on the local machine.
        /// </summary>
        public string LocalFilename { get; }

        /// <summary>
        /// Gets an identifier that uniquely represent a file on the remote cloud.
        /// </summary>
        public string RemoteFileIdentifier { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localFilename"></param>
        /// <param name="remoteFileIdentifier"></param>
        public CloudStorageFileInfo(string localFilename, string remoteFileIdentifier)
        {
            LocalFilename = localFilename;
            RemoteFileIdentifier = remoteFileIdentifier;
        }
    }

    /// <summary>
    /// Represents an application-independent cloud storage.
    /// </summary>
    public interface ICloudStorage : IDisposable
    {
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
        /// 
        /// </summary>
        /// <param name="remoteFileIdentifier"></param>
        /// <returns></returns>
        Task<Stream> Download(string remoteFileIdentifier);

        /// <summary>
        /// Uploads a local file to the cloud storage.
        /// </summary>
        /// <param name="localFilename">The filename of the local file to upload.</param>
        /// <param name="stream">A readable stream containing the local file content to upload to the cloud storage.</param>
        /// <returns>Returns a task to be awaited until upload is done, true meaning success and false meaning a failure occured.</returns>
        Task<bool> Upload(string localFilename, Stream stream);

        /// <summary>
        /// Delete a remote file on the cloud storage.
        /// </summary>
        /// <param name="remoteFileIdentifier">The identifier of the remote file to delete.</param>
        /// <returns>Returns a task to be awaited until delteion is done, true meaning success and false meaning a failure occured.</returns>
        Task<bool> Delete(string remoteFileIdentifier);
    }
}
