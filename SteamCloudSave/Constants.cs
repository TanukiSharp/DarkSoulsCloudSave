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
        public const string GameDisplayName = "Dark Souls 3";
        public const string GameSteamUrl = "steam://rungameid/374320";
        public const string ProcessName = "DarkSoulsIII";
        public const string SaveDataPath = "%APPDATA%/DarkSoulsIII";
        public const ArchiveMode GameArchiveMode = ArchiveMode.SubFolders;
    }

    public static class GoogleDriveConstants
    {
        public const string ApplicationName = "DarkSoulsCloudSave";
        public const string ClientId = "483903211848-lll6sv6teesjlvrnu2faobrgvse17h5e.apps.googleusercontent.com";
        public const string ClientSecret = "2qatYztspPDByeq4rh7KBi_I";
    }

    public static class DropboxConstants
    {
        public const string AppKey = "cwoecqgt2xtma0l";
        public const string AppSecret = "2a3si3j0kvgrush";
    }
}
