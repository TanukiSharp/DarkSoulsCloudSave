﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsCloudSave.Core
{
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
        /// <returns>Returns an array of remote filenames.</returns>
        Task<string[]> ListFiles();

        /// <summary>
        /// Downloads a remote file from the cloud storage as a readable stream.
        /// </summary>
        /// <param name="fileIdentifier">The identifier of the remote file to download from the cloud storage.</param>
        /// <returns>Returns a readable stream representing the remote file to download.</returns>
        Task<Stream> Download(string fileIdentifier);

        /// <summary>
        /// Uploads a local file to the cloud storage.
        /// </summary>
        /// <param name="fileIdentifier">The identifier to be given to the remote file.</param>
        /// <param name="stream">A readable stream containing the local file content to upload to the cloud storage.</param>
        /// <returns>Returns a task to be awaited until upload is done, true meaning success and false meaning a failure occured.</returns>
        Task<bool> Upload(string fileIdentifier, Stream stream);

        /// <summary>
        /// Delete a remote file on the cloud storage.
        /// </summary>
        /// <param name="fileIdentifier">The identifier of the remote file to delete.</param>
        /// <returns>Returns a task to be awaited until delteion is done, true meaning success and false meaning a failure occured.</returns>
        Task<bool> Delete(string fileIdentifier);
    }
}
