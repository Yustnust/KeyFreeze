using System;
using System.IO;
using System.Text;

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
    private static readonly string FilePath = Path.Combine(Folder, "config.cfg");

    public static Settings Load()
    {
        var s = new Settings();
        try
        {
            if (File.Exists(FilePath))
            {
                foreach (var raw in File.ReadAllLines(FilePath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var key = line.Substring(0, eq).Trim();
                    var val = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "Passphrase": s.Passphrase = val; break;
                        case "ComboKey": if (val.Length == 1) s.ComboKey = char.ToUpperInvariant(val[0]); break;
                        case "ComboHoldMs": if (int.TryParse(val, out var h)) s.ComboHoldMs = h; break;
                        case "PassphraseTimeoutMs": if (int.TryParse(val, out var p)) s.PassphraseTimeoutMs = p; break;
                        case "AutoLockIdle": s.AutoLockIdle = val == "1" || val == "true"; break;
                        case "AutoLockIdleMinutes": if (int.TryParse(val, out var m)) s.AutoLockIdleMinutes = m; break;
                        case "ShowOverlay": s.ShowOverlay = val == "1" || val == "true"; break;
                        case "EnableWatchdog": s.EnableWatchdog = val == "1" || val == "true"; break;
                    }
                }
            }
        }
        catch { }
        return s;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var sb = new StringBuilder();
            sb.AppendLine("# KeyFreeze settings");
            sb.AppendLine("Passphrase=" + Passphrase);
            sb.AppendLine("ComboKey=" + (char)ComboKey);
            sb.AppendLine("ComboHoldMs=" + ComboHoldMs);
            sb.AppendLine("PassphraseTimeoutMs=" + PassphraseTimeoutMs);
            sb.AppendLine("AutoLockIdle=" + (AutoLockIdle ? "1" : "0"));
            sb.AppendLine("AutoLockIdleMinutes=" + AutoLockIdleMinutes);
            sb.AppendLine("ShowOverlay=" + (ShowOverlay ? "1" : "0"));
            sb.AppendLine("EnableWatchdog=" + (EnableWatchdog ? "1" : "0"));
            File.WriteAllText(FilePath, sb.ToString());
        }
        catch { }
    }
}
