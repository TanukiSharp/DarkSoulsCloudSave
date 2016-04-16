using Dropbox.Api;
using Dropbox.Api.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DarkSoulsCloudSave.Core.CloudStorages
{
    /// <summary>
    /// Implementation of cloud storage for the Dropbox platform.
    /// </summary>
    public class DropboxCloudStorage : ICloudStorage
    {
        private DropboxClient dropboxClient;
        private IReadOnlyDictionary<string, string> config;

        private const string AppKey = "cwoecqgt2xtma0l";
        private const string AppSecret = "2a3si3j0kvgrush"; // <- not that secret in that case

        /// <summary>
        /// Initializes the Dropbox library, and ignites the authorization process if needed.
        /// </summary>
        /// <returns>Returns a task to be awaited until the initialization process is done.</returns>
        public async Task Initialize()
        {
            string accessToken;

            config = ConfigurationUtility.ReadConfigurationFile(GetType());

            if (config.TryGetValue("AccessToken", out accessToken) && string.IsNullOrWhiteSpace(accessToken) == false)
                accessToken = SecurityUtility.UnprotectString(accessToken, DataProtectionScope.CurrentUser);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Uri authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(AppKey, false);
                var url = authorizeUri.ToString();

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
                    MessageBox.Show("An error occured:\r\n" + ex.Message, "Authorization Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OAuth2Response response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code, AppKey, AppSecret);

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
        /// Lists the files available in the 'Apps/DarkSoulsCloudStrorage' remote folder on the Dropbox.
        /// </summary>
        /// <returns>Returns an array of remote filenames.</returns>
        public async Task<string[]> ListFiles()
        {
            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            var list = await dropboxClient.Files.ListFolderAsync(string.Empty);

            return list.Entries
                .Where(e => e.IsFile)
                .Where(e => String.Equals(Path.GetExtension(e.Name), ".zip", StringComparison.InvariantCultureIgnoreCase))
                .Select(e => e.PathDisplay)
                .ToArray();
        }

        /// <summary>
        /// Downloads a remote file from Dropbox, as a readable stream.
        /// </summary>
        /// <param name="fullFilename">The full filename of the remote file to download from Dropbox.</param>
        /// <returns>Returns a readable stream representing the remote file to download.</returns>
        public async Task<Stream> Download(string fullFilename)
        {
            if (string.IsNullOrWhiteSpace(fullFilename))
                throw new ArgumentException($"Invalid '{nameof(fullFilename)}' argument.", nameof(fullFilename));

            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            using (var response = await dropboxClient.Files.DownloadAsync(fullFilename))
                return new MemoryStream(await response.GetContentAsByteArrayAsync());
        }

        /// <summary>
        /// Uploads a local file to Dropbox.
        /// </summary>
        /// <param name="fullFilename">The full filename to be given to the remote file.</param>
        /// <param name="stream">A readable stream containing the local file content to upload to Dropbox.</param>
        /// <returns>Returns a task to be awaited until upload is done.</returns>
        public async Task Upload(string fullFilename, Stream stream)
        {
            if (string.IsNullOrWhiteSpace(fullFilename))
                throw new ArgumentException($"Invalid '{nameof(fullFilename)}' argument.", nameof(fullFilename));

            if (stream == null || stream.CanRead == false)
                throw new ArgumentException($"Invalid '{nameof(stream)}' argument. It must be a valid instance and being readable.", nameof(stream));

            if (dropboxClient == null)
                throw new InvalidOperationException("Not initialized");

            await dropboxClient.Files.UploadAsync(fullFilename, WriteMode.Overwrite.Instance, body: stream);
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
