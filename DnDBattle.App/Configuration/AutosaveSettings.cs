using System.IO;

namespace DnDBattle.App.Configuration;

public sealed class AutosaveSettings
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300; // 5 minutes
    public int MaxBackups { get; set; } = 5;
    public string SaveDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "DnDBattle", "Saves");
}
