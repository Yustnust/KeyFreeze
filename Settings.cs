using System;
using System.IO;
using System.Text.Json;

namespace KeyFreeze;

public sealed class Settings
{
    public string Passphrase { get; set; } = "letmein";
    public int ComboKey { get; set; } = 0x4C;
    public int ComboHoldMs { get; set; } = 1500;
    public int PassphraseTimeoutMs { get; set; } = 3000;
    public bool AutoLockIdle { get; set; } = true;
    public int AutoLockIdleMinutes { get; set; } = 5;
    public bool ShowOverlay { get; set; } = true;
    public bool EnableWatchdog { get; set; } = true;

    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeyFreeze");
    private static readonly string FilePath = Path.Combine(Folder, "config.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath));
                if (s != null) return s;
            }
        }
        catch { }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
