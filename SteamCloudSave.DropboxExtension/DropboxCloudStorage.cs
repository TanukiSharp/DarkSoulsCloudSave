using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using SteamCloudSave.Core;

namespace SteamCloudSave.DropboxExtension;

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
            accessToken = SecurityUtility.UnprotectString(accessToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            accessToken = await ObtainAccessToken();

            ConfigurationUtility.CreateConfigurationFile(GetType(), new Dictionary<string, string>
            {
                { "AccessToken", SecurityUtility.ProtectString(accessToken) },
            });
        }

        dropboxClient = new DropboxClient(accessToken);
    }

    private async Task<string> ObtainAccessToken()
    {
        string codeVerifier = DropboxOAuth2Helper.GeneratePKCECodeVerifier();
        string codeChallenge = DropboxOAuth2Helper.GeneratePKCECodeChallenge(codeVerifier);

        const string redirectUri = "http://localhost:51515/";

        Uri authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, appKey, redirectUri, state: null, codeChallenge: codeChallenge);

        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(redirectUri);
        httpListener.Start();

        Process.Start(new ProcessStartInfo(authorizeUri.AbsoluteUri) { UseShellExecute = true });

        HttpListenerContext context = await httpListener.GetContextAsync();
        HttpListenerResponse response = context.Response;
        string responseString = """<html><body style="font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;font-size:2rem;">You may now close this page and return to the application.</body></html>""";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        Stream responseOutput = response.OutputStream;
        await responseOutput.WriteAsync(buffer);
        responseOutput.Close();
        httpListener.Stop();

        OAuth2Response tokenResponse = await DropboxOAuth2Helper.ProcessCodeFlowAsync(context.Request.Url, appKey, appSecret, redirectUri: redirectUri, codeVerifier: codeVerifier);

        return tokenResponse.AccessToken;
    }

    /// <summary>
    /// Lists the files available in the Apps remote folder on the Dropbox.
    /// </summary>
    /// <returns>Returns an array of remote filenames.</returns>
    public async Task<IReadOnlyList<CloudStorageFileInfo>> ListFiles()
    {
        if (dropboxClient is null)
            throw new InvalidOperationException("Not initialized");

        ListFolderResult list = await dropboxClient.Files.ListFolderAsync(string.Empty);

        return list.Entries
            .Where(e => e.IsFile && e.IsDeleted == false)
            .Where(e => string.Equals(Path.GetExtension(e.Name), ".zip", StringComparison.InvariantCultureIgnoreCase))
            .Select(e => CloudStorageFileInfo.ParseCloudStorageFileInfo(e.Name, e.AsFile.Id))
            .ToList();
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

        if (dropboxClient is null)
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

        if (stream is null || stream.CanRead == false)
            throw new ArgumentException($"Invalid '{nameof(stream)}' argument. It must be a valid instance and being readable.", nameof(stream));

        if (dropboxClient is null)
            throw new InvalidOperationException("Not initialized");

        var uploadArg = new UploadArg(localFilename, WriteMode.Overwrite.Instance, autorename: false, mute: true);

        await dropboxClient.Files.UploadAsync(uploadArg, stream);

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

        if (dropboxClient is null)
            throw new InvalidOperationException("Not initialized");

        DeleteResult result;

        try
        {
            result = await dropboxClient.Files.DeleteV2Async($"/{fileInfo.LocalFilename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }

        return result.Metadata?.AsFile?.Id == fileInfo.RemoteFileIdentifier;
    }

    /// <summary>
    /// Delete multiple remote files from Dropbox.
    /// </summary>
    /// <param name="fileInfo">The file information representing the remote files to delete.</param>
    /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
    public async Task<bool> DeleteMany(IEnumerable<CloudStorageFileInfo> fileInfo)
    {
        if (dropboxClient is null)
            throw new InvalidOperationException("Not initialized");

        DeleteBatchLaunch result = await dropboxClient.Files.DeleteBatchAsync(fileInfo.Select(x => new DeleteArg($"/{x.LocalFilename}")));

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
        GC.SuppressFinalize(this);

        if (dropboxClient is not null)
        {
            dropboxClient.Dispose();
            dropboxClient = null;
        }
    }
}
