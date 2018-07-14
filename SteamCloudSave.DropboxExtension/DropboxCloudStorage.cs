using SteamCloudSave.Core;
using Dropbox.Api;
using Dropbox.Api.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using Dropbox.Api.Stone;

namespace SteamCloudSave.DropboxExtension
{
    /// <summary>
    /// Implementation of cloud storage for the Dropbox platform.
    /// </summary>
    public class DropboxCloudStorage : ICloudStorage
    {
        private DropboxClient dropboxClient;

        private readonly string appKey;
        private readonly string appSecret;

        /// <summary>
        /// Gets the display name of the current <see cref="ICloudStorage"/> instance.
        /// </summary>
        public string Name => "Dropbox";

        /// <summary>
        /// Initializes the <see cref="DropboxCloudStorage"/> instance.
        /// </summary>
        /// <param name="appKey">The Dropbox application key.</param>
        /// <param name="appSecret">The Dropbox application secret.</param>
        public DropboxCloudStorage(string appKey, string appSecret)
        {
            this.appKey = appKey;
            this.appSecret = appSecret;
        }

        /// <summary>
        /// Initializes the Dropbox library, and ignites the authorization process if needed.
        /// </summary>
        /// <returns>Returns a task to be awaited until the initialization process is done.</returns>
        public async Task Initialize()
        {
            string accessToken;

            IReadOnlyDictionary<string, string> config = ConfigurationUtility.ReadConfigurationFile(GetType());

            if (config.TryGetValue("AccessToken", out accessToken) && string.IsNullOrWhiteSpace(accessToken) == false)
                accessToken = SecurityUtility.UnprotectString(accessToken, DataProtectionScope.CurrentUser);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Uri authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(appKey, false);
                string url = authorizeUri.ToString();

                MessageBox.Show("After you click the OK button on this dialog, a web page asking you to allow the application will open, and then another one containing a code.\r\n\r\nOnce you see the code, please copy it to the clipboard by selecting it and pressing Ctrl+C, or right click and 'Copy' menu.", "Authorization", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(url);
                MessageBox.Show("Please proceed by closing this dialog once you copied the code.", "Authorization", MessageBoxButton.OK, MessageBoxImage.Information);

                string code = null;

                try
                {
                    code = Clipboard.GetText();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured:\r\n" + ex.Message, "Authorization failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OAuth2Response response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code, appKey, appSecret);

                accessToken = response.AccessToken;

                ConfigurationUtility.CreateConfigurationFile(GetType(), new Dictionary<string, string>
                {
                    { "AccessToken", SecurityUtility.ProtectString(accessToken, DataProtectionScope.CurrentUser) },
                });

                MessageBox.Show("Authorization process succeeded.", "Authorization", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            dropboxClient = new DropboxClient(accessToken);
        }

        /// <summary>
        /// Lists the files available in the Apps remote folder on the Dropbox.
        /// </summary>
        /// <returns>Returns an array of remote filenames.</returns>
        public async Task<IEnumerable<CloudStorageFileInfo>> ListFiles()
        {
            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            ListFolderResult list = await dropboxClient.Files.ListFolderAsync(string.Empty);

            return list.Entries
                .Where(e => e.IsFile && e.IsDeleted == false)
                .Where(e => string.Equals(Path.GetExtension(e.Name), ".zip", StringComparison.InvariantCultureIgnoreCase))
                .Select(e => CloudStorageFileInfo.ParseCloudStorageFileInfo(e.Name, e.AsFile.Id))
                .ToArray();
        }

        /// <summary>
        /// Downloads a remote file from Dropbox, as a readable stream.
        /// </summary>
        /// <param name="fileInfo">The file information representing the remote file to download from Dropbox.</param>
        /// <returns>Returns a readable stream representing the remote file to download.</returns>
        public async Task<Stream> Download(CloudStorageFileInfo fileInfo)
        {
            if (string.IsNullOrWhiteSpace(fileInfo.RemoteFileIdentifier))
                throw new ArgumentException($"Invalid '{nameof(fileInfo)}' argument.", nameof(fileInfo));

            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            IDownloadResponse<FileMetadata> response = await dropboxClient.Files.DownloadAsync(fileInfo.RemoteFileIdentifier);
            return await response.GetContentAsStreamAsync();
        }

        /// <summary>
        /// Uploads a local file to Dropbox.
        /// </summary>
        /// <param name="localFilename">The filename of the local file.</param>
        /// <param name="stream">A readable stream containing the local file content to upload to Dropbox.</param>
        /// <returns>Returns a task to be awaited until upload is done.</returns>
        public async Task<bool> Upload(string localFilename, Stream stream)
        {
            if (string.IsNullOrWhiteSpace(localFilename))
                throw new ArgumentException($"Invalid '{nameof(localFilename)}' argument.", nameof(localFilename));

            if (stream == null || stream.CanRead == false)
                throw new ArgumentException($"Invalid '{nameof(stream)}' argument. It must be a valid instance and being readable.", nameof(stream));

            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            var commitInfo = new CommitInfo(localFilename, WriteMode.Overwrite.Instance, false, null, true);

            FileMetadata result = await dropboxClient.Files.UploadAsync(commitInfo, stream);

            return true;
        }

        /// <summary>
        /// Delete a remote file from Dropbox.
        /// </summary>
        /// <param name="fileInfo">The identifier of the remote file to delete.</param>
        /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
        public async Task<bool> Delete(CloudStorageFileInfo fileInfo)
        {
            if (string.IsNullOrWhiteSpace(fileInfo.LocalFilename))
                throw new ArgumentException($"Invalid '{nameof(fileInfo)}' argument.", nameof(fileInfo));

            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            Metadata result;

            try
            {
                result = await dropboxClient.Files.DeleteAsync("/" + fileInfo.LocalFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return result?.AsFile?.Id == fileInfo.RemoteFileIdentifier;
        }

        /// <summary>
        /// Delete multiple remote files from Dropbox.
        /// </summary>
        /// <param name="fileInfo">The file information representing the remote files to delete.</param>
        /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
        public async Task<bool> DeleteMany(IEnumerable<CloudStorageFileInfo> fileInfo)
        {
            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            DeleteBatchLaunch result = await dropboxClient.Files.DeleteBatchAsync(fileInfo.Select(x => new DeleteArg("/" + x.LocalFilename)));

            int trials = 0;

            while (true)
            {
                DeleteBatchJobStatus status = await dropboxClient.Files.DeleteBatchCheckAsync(result.AsAsyncJobId.Value);
                if (status.IsComplete)
                    return status.AsComplete.Value.Entries.All(x => x.IsSuccess);

                await Task.Delay(500);

                if (trials++ > 10)
                    break;
            }

            return false;
        }

        /// <summary>
        /// Disposes the Dropbox library.
        /// </summary>
        public void Dispose()
        {
            if (dropboxClient != null)
            {
                dropboxClient.Dispose();
                dropboxClient = null;
            }
        }
    }
}
