namespace SteamCloudSave.DataModels.Configuration.Configuration.V1;

public class Configuration : ConfigurationBase<Configuration>
{
    public bool AutoRestore { get; set; }
    public bool AutoStore { get; set; }
    public int RevisionsToKeep { get; set; } = 5;
    public string? RestoreCloudStorage { get; set; }
    public string[]? StoreCloudStorages { get; set; }
}
