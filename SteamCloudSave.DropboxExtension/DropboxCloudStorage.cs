using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using SteamCloudSave.Core;

namespace SteamCloudSave.DropboxExtension;

internal readonly record struct Tokens(string AccessToken, string RefreshToken, DateTime? ExpiresAt);

/// <summary>
/// Implementation of cloud storage for the Dropbox platform.
/// </summary>
public class DropboxCloudStorage : ICloudStorage
{
    private DropboxClient dropboxClient = null!;

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
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? accessToken;
        string? refreshToken;
        string? expiresAt;
        DateTime expiresAtDateTime;

        IReadOnlyDictionary<string, string?> config = ConfigurationUtility.ReadConfigurationFile(GetType());

        if (config.TryGetValue("AccessToken", out accessToken) && string.IsNullOrWhiteSpace(accessToken) == false)
        {
            accessToken = SecurityUtility.UnprotectString(accessToken);
        }

        if (config.TryGetValue("RefreshToken", out refreshToken) && string.IsNullOrWhiteSpace(refreshToken) == false)
        {
            refreshToken = SecurityUtility.UnprotectString(refreshToken);
        }

        config.TryGetValue("ExpiresAt", out expiresAt);

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            Tokens tokens = await ObtainTokensAsync(cancellationToken);

            accessToken = tokens.AccessToken;
            refreshToken = tokens.RefreshToken;
            expiresAt = tokens.ExpiresAt?.ToString("o", CultureInfo.InvariantCulture);

            ConfigurationUtility.CreateConfigurationFile(GetType(), new Dictionary<string, string?>
            {
                { "AccessToken", SecurityUtility.ProtectString(accessToken) },
                { "RefreshToken", SecurityUtility.ProtectString(refreshToken) },
                { "ExpiresAt", expiresAt },
            });
        }

        _ = DateTime.TryParse(expiresAt, out expiresAtDateTime);

        dropboxClient = new DropboxClient(accessToken, refreshToken, expiresAtDateTime, appKey);
    }

    private async Task<Tokens> ObtainTokensAsync(CancellationToken cancellationToken)
    {
        string codeVerifier = DropboxOAuth2Helper.GeneratePKCECodeVerifier();
        string codeChallenge = DropboxOAuth2Helper.GeneratePKCECodeChallenge(codeVerifier);

        const string redirectUri = "http://localhost:51515/";

        Uri authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(
            OAuthResponseType.Code,
            appKey,
            redirectUri,
            state: null,
            codeChallenge: codeChallenge,
            tokenAccessType: TokenAccessType.Offline
        );

        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(redirectUri);
        httpListener.Start();

        Process.Start(new ProcessStartInfo(authorizeUri.AbsoluteUri) { UseShellExecute = true });

        HttpListenerContext context = await httpListener.GetContextAsync().WaitAsync(cancellationToken);
        HttpListenerResponse response = context.Response;
        string responseString = """<html><body style="font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;font-size:1.5rem;">You may now close this page and return to the application.</body></html>""";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        Stream responseOutput = response.OutputStream;
        await responseOutput.WriteAsync(buffer, cancellationToken);
        responseOutput.Close();
        httpListener.Stop();

        OAuth2Response tokenResponse = await DropboxOAuth2Helper
            .ProcessCodeFlowAsync(context.Request.Url, appKey, appSecret, redirectUri: redirectUri, codeVerifier: codeVerifier)
            .WaitAsync(cancellationToken);

        return new Tokens(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresAt);
    }

    /// <summary>
    /// Lists the files available in the Apps remote folder on the Dropbox.
    /// </summary>
    /// <returns>Returns an array of remote filenames.</returns>
    public async Task<IReadOnlyList<CloudStorageFileInfo>> ListFilesAsync(string path, CancellationToken cancellationToken)
    {
        if (dropboxClient is null)
        {
            throw new InvalidOperationException("Not initialized");
        }

        ListFolderResult list;

        try
        {
            list = await dropboxClient.Files.ListFolderAsync(path).WaitAsync(cancellationToken);
        }
        catch (ApiException<ListFolderError> ex) when (ex.ErrorResponse.AsPath?.Value.IsNotFound ?? false)
        {
            return [];
        }

        return list.Entries
            .Where(e => e.IsFile && e.IsDeleted == false)
            .Where(e => string.Equals(Path.GetExtension(e.Name), ".zip", StringComparison.OrdinalIgnoreCase))
            .Select(e => CloudStorageFileInfo.ParseCloudStorageFileInfo($"{path}/{e.Name}", e.AsFile.Id))
            .ToList();
    }

    /// <summary>
    /// Downloads a remote file from Dropbox, as a readable stream.
    /// </summary>
    /// <param name="fileInfo">The file information representing the remote file to download from Dropbox.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a readable stream representing the remote file to download.</returns>
    public async Task<Stream> DownloadAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileInfo.RemoteFileIdentifier))
        {
            throw new ArgumentException($"Invalid '{nameof(fileInfo)}' argument.", nameof(fileInfo));
        }

        if (dropboxClient is null)
        {
            throw new InvalidOperationException("Not initialized");
        }

        IDownloadResponse<FileMetadata> response = await dropboxClient.Files
            .DownloadAsync(fileInfo.RemoteFileIdentifier)
            .WaitAsync(cancellationToken);

        return await response.GetContentAsStreamAsync().WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Uploads a local file to Dropbox.
    /// </summary>
    /// <param name="remoteFilename">The full filename to be given to the remote file.</param>
    /// <param name="stream">A readable stream containing the local file content to upload to Dropbox.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until upload is done.</returns>
    public async Task<bool> UploadAsync(string remoteFilename, Stream stream, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remoteFilename))
        {
            throw new ArgumentException($"Invalid '{nameof(remoteFilename)}' argument.", nameof(remoteFilename));
        }

        if (stream is null || stream.CanRead == false)
        {
            throw new ArgumentException($"Invalid '{nameof(stream)}' argument. It must be a valid instance and being readable.", nameof(stream));
        }

        if (dropboxClient is null)
        {
            throw new InvalidOperationException("Not initialized");
        }

        var uploadArg = new UploadArg(remoteFilename, WriteMode.Overwrite.Instance, autorename: false, mute: true);

        await dropboxClient.Files.UploadAsync(uploadArg, stream).WaitAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Delete a remote file from Dropbox.
    /// </summary>
    /// <param name="fileInfo">The identifier of the remote file to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
    public async Task<bool> DeleteAsync(CloudStorageFileInfo fileInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileInfo.LocalFilename))
        {
            throw new ArgumentException($"Invalid '{nameof(fileInfo)}' argument.", nameof(fileInfo));
        }

        if (dropboxClient is null)
        {
            throw new InvalidOperationException("Not initialized");
        }

        DeleteResult result;

        try
        {
            result = await dropboxClient.Files.DeleteV2Async(fileInfo.LocalFilename).WaitAsync(cancellationToken);
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
    /// <param name="perFileTimeout"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a task to be awaited until deletion is done, true meaning success and false meaning a failure occured.</returns>
    public async Task<bool> DeleteManyAsync(IEnumerable<CloudStorageFileInfo> fileInfo, TimeSpan perFileTimeout, CancellationToken cancellationToken)
    {
        if (dropboxClient is null)
        {
            throw new InvalidOperationException("Not initialized");
        }

        TimeSpan totalTimeout = TimeSpan.Zero;
        List<DeleteArg> deletions = [];

        foreach (CloudStorageFileInfo f in fileInfo)
        {
            deletions.Add(new DeleteArg(f.LocalFilename));
            totalTimeout += perFileTimeout;
            perFileTimeout *= 0.75;
        }

        using var cts = new CancellationTokenSource(totalTimeout);
        using CancellationTokenSource ct = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        DeleteBatchLaunch result = await dropboxClient.Files
            .DeleteBatchAsync(deletions)
            .WaitAsync(ct.Token);

        int trials = 0;

        while (true)
        {
            DeleteBatchJobStatus status = await dropboxClient.Files
                .DeleteBatchCheckAsync(result.AsAsyncJobId.Value)
                .WaitAsync(ct.Token);

            if (status.IsComplete)
            {
                return status.AsComplete.Value.Entries.All(x => x.IsSuccess);
            }

            await Task.Delay(500, ct.Token);

            if (trials++ > 10)
            {
                break;
            }
        }

        return false;
    }

    /// <summary>
    /// Disposes the Dropbox library.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (dropboxClient is not null)
        {
            dropboxClient.Dispose();
            dropboxClient = null!;
        }

        return ValueTask.CompletedTask;
    }
}
