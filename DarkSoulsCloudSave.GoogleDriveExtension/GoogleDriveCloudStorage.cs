using DarkSoulsCloudSave.Core;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data = Google.Apis.Drive.v3.Data;

namespace DarkSoulsCloudSave.GoogleDriveExtension
{
    /// <summary>
    /// Implementation of cloud storage for the Google Drive platform. (AppData access variant)
    /// </summary>
    public class GoogleDriveCloudStorage : ICloudStorage
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveAppdata };
        private static readonly string ApplicationName = "DarkSoulsCloudSave";

        private const string ClientId = "483903211848-lll6sv6teesjlvrnu2faobrgvse17h5e.apps.googleusercontent.com";
        private const string ClientSecret = "2qatYztspPDByeq4rh7KBi_I";

        private DriveService driveService;

        /// <summary>
        /// Initializes the Google Drive library, and ignites the authorization process if needed.
        /// </summary>
        /// <returns>Returns a task to be awaited until the initialization process is done.</returns>
        public async Task Initialize()
        {
            var path = ConfigurationUtility.GetExtensionConfigurationFilePath(GetType());
            path = Path.GetDirectoryName(path);

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                },
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(path, true));

            driveService = new DriveService(new BaseClientService.Initializer()
            {
                
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        /// <summary>
        /// Lists the files available in the remote app folder on the Google Drive.
        /// </summary>
        /// <returns>Returns an array of remote filenames.</returns>
        public async Task<string[]> ListFiles()
        {
            FilesResource.ListRequest request = driveService.Files.List();
            request.Spaces = "appDataFolder";
            request.Fields = "files(id, name, createdTime)";

            // getting all files, then filtering does not scale :(
            var files = (await request.ExecuteAsync()).Files;

            var result = new List<string>();

            foreach (var fileGroup in files.GroupBy(x => x.Name))
            {
                var newest = fileGroup.OrderByDescending(x => x.CreatedTime).FirstOrDefault();
                if (newest != null)
                    result.Add(newest.Id);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Downloads a remote file from Google Drive, as a readable stream.
        /// </summary>
        /// <param name="fullFilename">The Id of the remote file to download from Google Drive.</param>
        /// <returns>Returns a readable stream representing the remote file to download.</returns>
        public async Task<Stream> Download(string fullFilename)
        {
            var stream = new MemoryStream();

            FilesResource.GetRequest request = driveService.Files.Get(fullFilename);

            await request.DownloadAsync(stream);

            return stream;
        }

        /// <summary>
        /// Uploads a local file to Google Drive.
        /// </summary>
        /// <param name="fullFilename">The full filename to be given to the remote file.</param>
        /// <param name="stream">A readable stream containing the local file content to upload to Google Drive.</param>
        /// <returns>Returns a task to be awaited until upload is done.</returns>
        public async Task Upload(string fullFilename, Stream stream)
        {
            var fileMetadata = new Data.File
            {
                Name = fullFilename,
                Parents = new List<string>() { "appDataFolder" }
            };

            var request = driveService.Files.Create(fileMetadata, stream, "application/octet-stream");

            var result = await request.UploadAsync();

        }

        /// <summary>
        /// Disposes the Google Drive library.
        /// </summary>
        public void Dispose()
        {
            if (driveService != null)
            {
                driveService.Dispose();
                driveService = null;
            }
        }
    }
}
