using System.IO;
using System.Xml.Serialization;

namespace SteamCloudSave.Windows.DataModels;

public abstract class ConfigurationBase<T> where T : ConfigurationBase<T>
{
    [XmlIgnore]
    public string SettingsFilePath { get; private set; }

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

        var settings = (T)serializer.Deserialize(fs);

        settings.SettingsFilePath = filename;

        return settings;
    }
}
