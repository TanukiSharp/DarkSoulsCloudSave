using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamCloudSave.Core;

namespace SteamCloudSave
{
    public static class Constants
    {
        public const string GameDisplayName = "Monster Hunter World";
        public const string GameSteamUrl = "steam://rungameid/374320";
        public const string ProcessName = "DarkSoulsIII";
        public const string SaveDataPath = "%APPDATA%/DarkSoulsIII";
        public const ArchiveMode GameArchiveMode = ArchiveMode.SubFolders;
    }

    public static class GoogleDriveConstants
    {
        public const string ApplicationName = "MHWCloudSave";
        public const string ClientId = "803098234462-27l97m8b5ted1s712qqaq898tr8n1evt.apps.googleusercontent.com";
        public const string ClientSecret = "7vylSZ-26OOfXHu63Mi1T-63";
    }

    public static class DropboxConstants
    {
        public const string AppKey = "9rvmwx8llat942t";
        public const string AppSecret = "be3ilql1pdsnylk";
    }
}
