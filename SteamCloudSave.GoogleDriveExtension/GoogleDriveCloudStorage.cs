using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using SteamCloudSave.Core;
using Data = Google.Apis.Drive.v3.Data;

namespace SteamCloudSave.GoogleDriveExtension;

/// <summary>
/// Implementation of cloud storage for the Google Drive platform. (AppData access variant)
/// </summary>
public class GoogleDriveCloudStorage : ICloudStorage
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveAppdata];
    private readonly string applicationName;

    private readonly string clientId;
    private readonly string clientSecret;

    private DriveService driveService = null!;

    /// <summary>
    /// Gets the display name of the current <see cref="ICloudStorage"/> instance.
    /// </summary>
    public string Name => "Google Drive";

    private static readonly char[] pathSeparators = ['/', '\\'];

    /// <summary>
    /// Initializes the <see cref="GoogleDriveCloudStorage"/> instance.
    /// </summary>
    /// <param name="applicationName">The Google Drive application name.</param>
    /// <param name="clientId">The Google Drive application client identifier.</param>
    /// <param name="clientSecret">The Google Drive application client secret.</param>
    public GoogleDriveCloudStorage(string applicationName, string clientId, string clientSecret)
    {
        this.applicationName = applicationName;
        this.clientId = clientId;
        this.clientSecret = clientSecret;
    }

    /// <summary>
    /// Initializes the Google Drive library, and ignites the authorization process if needed.
    /// </summary>
    /// <returns>Returns a task to be awaited until the initialization process is done.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? path = ConfigurationUtility.GetExtensionConfigurationFilePath(GetType());
        path = Path.GetDirectoryName(path);

        if (path is null)
        {
            throw new InvalidOperationException("Failed to determine ");
        }

        UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
            },
            Scopes,
            "user",
            cancellationToken,
            new FileDataStore(path, true));

        driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });
    }

    /// <summary>
    /// Lists the files available in the remote app folder on the Google Drive.
    /// </summary>
    /// <param name="path">The path where to list files.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns an array of remote files.</returns>
    public async Task<IReadOnlyList<CloudStorageFileInfo>> ListFilesAsync(string path, CancellationToken cancellationToken)
    {
        Data.File? directory = null;

        if (string.IsNullOrEmpty(path) == false)
        {
            directory = await GetDirectoryAsync(path, cancellationToken);

            if (directory is null)
            {
                return [];
            }
        }

        FilesResource.ListRequest request = driveService.Files.List();

        request.Q = $"'{directory?.Id ?? "appDataFolder"}' in parents";
        request.Spaces = "appDataFolder";
        request.Fields = "files(id, name, createdTime)";

        IList<Data.File> files;

        try
        {
            files = (await request.ExecuteAsync(cancellationToken)).Files;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        return files
            .Where(x => string.Equals(Path.GetExtension(x.Name), ".zip", StringComparison.OrdinalIgnoreCase))
            .Select(x => CloudStorageFileInfo.ParseCloudStorageFileInfo(x.Name, x.Id))
            .ToList();
    }

    /// <summary>
    /// Downloads a remote file from Google Drive, as a readable stream.
    /// </summary>
    /// <param name="fileInfo">The file identifier representing the remote file to download from Google Drive.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a readable stream representing the remote file to download.</returns>
    public async Task<Stream> DownloadAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken)
    {
        var stream = new MemoryStream();

        FilesResource.GetRequest request = driveService.Files.Get(fileInfo.RemoteFileIdentifier);

        await request.DownloadAsync(stream, cancellationToken);
        stream.Position = 0;

        return stream;
    }

    private async Task<Data.File> CreateDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        string[] pathParts = path.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string parentId = "appDataFolder";
        Data.File result = null!;

        for (int i = 0; i < pathParts.Length; i++)
        {
            Data.File folderMetadata = new()
            {
                Name = pathParts[i],
                MimeType = "application/vnd.google-apps.folder",
                Parents = [parentId],
            };

            FilesResource.CreateRequest request = driveService.Files.Create(folderMetadata);

            result = await request.ExecuteAsync(cancellationToken);
            parentId = result.Id;
        }

        return result;
    }

    private async Task<Data.File?> GetDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        string[] pathParts = path.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string parentId = "appDataFolder";
        Data.File? result = null;

        for (int i = 0; i < pathParts.Length; i++)
        {
            FilesResource.ListRequest fileListRequest = driveService.Files.List();

            fileListRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{pathParts[i]}' and '{parentId}' in parents";
            fileListRequest.Spaces = "appDataFolder";

            Data.FileList fileListResponse = await fileListRequest.ExecuteAsync(cancellationToken);

            if (fileListResponse.Files.Count == 0)
            {
                break;
            }

            result = fileListResponse.Files[0];
            parentId = result.Id;
        }

        return result;
    }

    /// <summary>
    /// Uploads a local file to Google Drive.
    /// </summary>
    /// <param name="remoteFilename">The full filename to be given to the remote file.</param>
    /// <param name="stream">A readable stream containing the local file content to upload to Google Drive.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until upload is done.</returns>
    public async Task<bool> UploadAsync(string remoteFilename, Stream stream, CancellationToken cancellationToken)
    {
        string? path = Path.GetDirectoryName(remoteFilename);

        Data.File? directory = null;

        if (string.IsNullOrEmpty(path) == false)
        {
            directory = await GetDirectoryAsync(path, cancellationToken);

            directory ??= await CreateDirectoryAsync(path, cancellationToken);
        }

        var fileMetadata = new Data.File
        {
            Name = Path.GetFileName(remoteFilename),
            Parents = [directory?.Id ?? "appDataFolder"]
        };

        FilesResource.CreateMediaUpload request = driveService.Files.Create(fileMetadata, stream, "application/octet-stream");

        IUploadProgress result = await request.UploadAsync(cancellationToken);

        if (result.Exception is not null)
        {
            throw result.Exception;
        }

        return result.Status == UploadStatus.Completed;
    }

    /// <summary>
    /// Deletes a file from Google Drive.
    /// </summary>
    /// <param name="fileInfo">The file information representing the remote file to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until delteion is done, true meaning success and false meaning a failure occured.</returns>
    public async Task<bool> DeleteAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken)
    {
        FilesResource.DeleteRequest request = driveService.Files.Delete(fileInfo.RemoteFileIdentifier);

        string result = await request.ExecuteAsync(cancellationToken);

        return result == string.Empty;
    }

    /// <summary>
    /// Deletes multiple remote files from Google Drive.
    /// </summary>
    /// <param name="fileInfo">The file information representing the remote files to delete.</param>
    /// <param name="perFileTimeout"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
    public async Task<bool> DeleteManyAsync(IEnumerable<CloudStorageFileInfo> fileInfo, TimeSpan perFileTimeout, CancellationToken cancellationToken)
    {
        bool[] results = await Task.WhenAll(fileInfo.Select(async x =>
        {
            using var cts = new CancellationTokenSource(perFileTimeout);
            return await DeleteAsync(x, cts.Token);
        })).WaitAsync(cancellationToken);

        return results.All(x => x);
    }

    /// <summary>
    /// Disposes the Google Drive library.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (driveService is not null)
        {
            driveService.Dispose();
            driveService = null!;
        }

        return ValueTask.CompletedTask;
    }
}
