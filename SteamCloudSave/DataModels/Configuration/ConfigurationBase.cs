using System;
using System.IO;
using System.Xml.Serialization;

namespace SteamCloudSave.DataModels.Configuration;

public abstract class ConfigurationBase<T> where T : ConfigurationBase<T>
{
    [XmlIgnore]
    public string SettingsFilePath { get; private set; } = null!;

    public void Save(string filename)
    {
        XmlSerializer serializer = new(typeof(T));

        using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);

        serializer.Serialize(fs, this);

        SettingsFilePath = filename;
    }

    public static T Load(string filename)
    {
        XmlSerializer serializer = new(typeof(T));

        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

        T? settings = (T?)serializer.Deserialize(fs);

        if (settings is null)
        {
            throw new InvalidOperationException($"Failed to deserialize XML from file '{filename}'.");
        }

        settings.SettingsFilePath = filename;

        return settings;
    }
}
