using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SteamCloudSave.Core;

/// <summary>
/// Provides helper methods related to configuration for Cloud Storage extensions.
/// </summary>
public static class ConfigurationUtility
{
    static ConfigurationUtility()
    {
        ExtensionsConfigurationPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "storageconfig"));

        if (Directory.Exists(ExtensionsConfigurationPath) == false)
        {
            Directory.CreateDirectory(ExtensionsConfigurationPath);
        }
    }

    /// <summary>
    /// Gets the full path where configuration files for extensions are located.
    /// </summary>
    public static string ExtensionsConfigurationPath { get; private set; }

    private static readonly IReadOnlyDictionary<string, string?> EmptyDictionary = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>());

    /// <summary>
    /// Gets the full filename of a configuration file for the given extension.
    /// </summary>
    /// <param name="extensionType">The class that implements the extension.</param>
    /// <returns>Returns the full filename of a configuration file for the given extension, or null if <paramref name="extensionType"/> is null.</returns>
    public static string? GetExtensionConfigurationFilePath(Type extensionType)
    {
        if (extensionType is null)
        {
            return null;
        }

        return Path.GetFullPath(Path.Combine(ExtensionsConfigurationPath, $"{extensionType.Name}.config"));
    }

    /// <summary>
    /// Creates a default extension configuration file.
    /// If one <paramref name="extensionType"/> or <paramref name="configuration"/> is null, or if <paramref name="configuration"/> is empty, nothing is done.
    /// </summary>
    /// <param name="extensionType">The class that implements the extension.</param>
    /// <param name="configuration">The key value pairs to store in the configuration file.</param>
    public static void CreateConfigurationFile(Type extensionType, IDictionary<string, string?> configuration)
    {
        if (extensionType is null || configuration is null || configuration.Count == 0)
        {
            return;
        }

        string? filePath = GetExtensionConfigurationFilePath(extensionType);

        if (filePath is null)
        {
            throw new InvalidOperationException($"Unknown extension type '{extensionType.FullName}'.");
        }

        var content = configuration
            .Where(kv => kv.Value is not null)
            .Select(kv => $"{kv.Key}={kv.Value}")
            .Aggregate((a, b) => string.Concat(a, '\n', b));

        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    private const char SharpCharacter = '#';
    private const char SemiColonCharacter = ';';
    private const string DoubleSlashString = "//";

    /// <summary>
    /// Reads the configuration file of a given extension.
    /// If <paramref name="extensionType"/> is null, an empty dictionary is returned.
    /// </summary>
    /// <param name="extensionType">The class that implements the extension.</param>
    /// <returns>Returns a dictionary containing the key/value pairs contained in the configuration file.
    /// If <paramref name="extensionType"/> is null, an empty dictionary is returned.</returns>
    public static IReadOnlyDictionary<string, string?> ReadConfigurationFile(Type extensionType)
    {
        if (extensionType is null)
        {
            return EmptyDictionary;
        }

        string? filePath = GetExtensionConfigurationFilePath(extensionType);

        if (filePath is null)
        {
            throw new InvalidOperationException($"Unknown extension type '{extensionType.FullName}'.");
        }

        if (File.Exists(filePath) == false)
        {
            File.Create(filePath).Close();
            return EmptyDictionary;
        }

        var dictionary = new Dictionary<string, string?>();

        foreach (string line in File.ReadAllLines(filePath).Select(x => x.TrimStart()))
        {
            if (line.StartsWith(SharpCharacter) || line.StartsWith(SemiColonCharacter) || line.StartsWith(DoubleSlashString))
            {
                continue;
            }

            string[] keyValue = line.Split(['='], 2);

            string key = keyValue[0].TrimEnd();

            if (key.Length == 0)
            {
                continue;
            }

            if (keyValue.Length == 1)
            {
                dictionary.Add(key, null);
            }
            else
            {
                dictionary.Add(key, keyValue[1]);
            }
        }

        return new ReadOnlyDictionary<string, string?>(dictionary);
    }
}
