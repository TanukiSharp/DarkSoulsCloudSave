using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsCloudSave.Core
{
    public static class SaveDataUtility
    {
        static SaveDataUtility()
        {
            SaveDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DarkSoulsIII");

            BackupsPath = Path.GetFullPath("./backups");
            if (Directory.Exists(BackupsPath) == false)
                Directory.CreateDirectory(BackupsPath);
        }

        public static string SaveDataPath { get; private set; }
        public static string BackupsPath { get; private set; }

        public static async Task<Stream> GetSaveDataArchive(string folderPath)
        {
            var stream = new MemoryStream();

            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true, Encoding.UTF8))
            {
                var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string filename = file.Substring(SaveDataPath.Length + 1, file.Length - SaveDataPath.Length - 1);
                    ZipArchiveEntry entry = archive.CreateEntry(filename, CompressionLevel.Optimal);
                    using (var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        using (Stream targetStream = entry.Open())
                            await sourceStream.CopyToAsync(targetStream);
                    }
                }
            }

            stream.Position = 0;
            return stream;
        }

        public static async Task ExtractSaveDataArchive(Stream archiveStream)
        {
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, false, Encoding.UTF8))
            {
                foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.Length > 0))
                {
                    string targetFilename = Path.Combine(SaveDataPath, entry.FullName);
                    string targetDirectory = Path.GetDirectoryName(targetFilename);
                    Directory.CreateDirectory(targetDirectory);

                    using (Stream sourceStream = entry.Open())
                    {
                        using (var targetStream = new FileStream(targetFilename, FileMode.OpenOrCreate, FileAccess.Write))
                            await sourceStream.CopyToAsync(targetStream);
                    }
                }
            }
        }

        public static async Task BackupLocalSaveData()
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");

            foreach (string directory in Directory.GetDirectories(SaveDataPath, "*", SearchOption.TopDirectoryOnly))
            {
                var filename = string.Format("{0}_{1}.zip", Path.GetFileName(directory), now);

                Stream archiveStream = await GetSaveDataArchive(directory);
                using (var targetStream = new FileStream(Path.Combine(BackupsPath, filename), FileMode.OpenOrCreate, FileAccess.Write))
                    await archiveStream.CopyToAsync(targetStream);
            }
        }
    }
}
