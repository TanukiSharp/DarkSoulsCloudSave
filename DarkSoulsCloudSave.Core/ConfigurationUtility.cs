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
    public static class ConfigurationUtility
    {
        static ConfigurationUtility()
        {
            ExtensionsConfigurationPath = Path.GetFullPath("./storageconfig");
            if (Directory.Exists(ExtensionsConfigurationPath) == false)
                Directory.CreateDirectory(ExtensionsConfigurationPath);
        }

        public static string ExtensionsConfigurationPath { get; private set; }

        private static readonly IReadOnlyDictionary<string, string> EmptyDictionary = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        public static string GetExtensionConfigurationFilePath(Type extensionType)
        {
            if (extensionType == null)
                return null;

            return Path.GetFullPath(Path.Combine(ExtensionsConfigurationPath, extensionType.Name + ".config"));
        }

        public static void CreateConfigurationFile(Type extensionType, IDictionary<string, string> configuration)
        {
            if (extensionType == null || configuration == null || configuration.Count == 0)
                return;

            var filePath = GetExtensionConfigurationFilePath(extensionType);

            var content = configuration
                .Select(kv => $"{kv.Key}={kv.Value}")
                .Aggregate((a, b) => string.Concat(a, Environment.NewLine, b));

            File.WriteAllText(filePath, content, Encoding.UTF8);
        }

        public static IReadOnlyDictionary<string, string> ReadConfigurationFile(Type extensionType)
        {
            if (extensionType == null)
                return EmptyDictionary;

            var filePath = GetExtensionConfigurationFilePath(extensionType);

            if (File.Exists(filePath) == false)
            {
                File.Create(filePath).Close();
                return EmptyDictionary;
            }

            var dictionary = new Dictionary<string, string>();

            foreach (string line in File.ReadAllLines(filePath).Select(x => x.TrimStart()))
            {
                if (line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("//"))
                    continue;

                string[] keyValue = line.Split(new char[] { '=' }, 2);

                string key = keyValue[0].TrimEnd();

                if (key.Length == 0)
                    continue;

                if (keyValue.Length == 1)
                    dictionary.Add(key, null);
                else
                    dictionary.Add(key, keyValue[1]);
            }

            return new ReadOnlyDictionary<string, string>(dictionary);
        }
    }
}
