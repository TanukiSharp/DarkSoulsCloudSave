using System;
using SteamCloudSave.Core;

namespace SteamCloudSave;

public static class Constants
{
    public const string GameDisplayName = "Dark Souls 3";
    public const string GameSteamUrl = "steam://rungameid/374320";
    public const string ProcessName = "DarkSoulsIII";
    public static readonly string[] WindowsSaveDataPath = ["%APPDATA%/DarkSoulsIII"];
    public static readonly string[] LinuxSaveDataPath = ["%HOME%/.steam/steam/steamapps/compatdata/374320/pfx/drive_c/users/steamuser/Application Data/DarkSoulsIII"];
    public const ArchiveMode GameArchiveMode = ArchiveMode.SubFolders;
}

public static class GoogleDriveConstants
{
    public const string ApplicationName = "DS3CloudSave";
    public const string ClientId = "707047029934-hvm9ai6s0t0bp75r8okgu5a53dq9emvq.apps.googleusercontent.com";
    public const string ClientSecret = "KQzJTLDsLftZZO2k5xdzuy8Z";
}

public static class DropboxConstants
{
    public const string AppKey = "cwoecqgt2xtma0l";
    public const string AppSecret = "2a3si3j0kvgrush";
}

public static class Timeouts
{
    public static readonly TimeSpan GameStartTimeout = TimeSpan.FromSeconds(20.0);
    public static readonly TimeSpan InitializeTimeout = TimeSpan.FromSeconds(30.0);
    public static readonly TimeSpan ListFilesTimeout = TimeSpan.FromSeconds(20.0);
    public static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(30.0);
    public static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(60.0);
    public static readonly TimeSpan DeleteTimeout = TimeSpan.FromSeconds(20.0);
}
