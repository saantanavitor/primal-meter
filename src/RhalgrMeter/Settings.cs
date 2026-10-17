using System.IO;
using System.Text.Json;

namespace RhalgrMeter;

/// <summary>Preferências da janela, salvas em %APPDATA%\RhalgrMeter\settings.json.</summary>
public sealed class Settings
{
    public static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RhalgrMeter");
    static string FilePath => Path.Combine(Folder, "settings.json");

    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 380;
    public double Height { get; set; } = 480;
    public string WsUrl { get; set; } = "ws://127.0.0.1:10501/ws";
    public bool Locked { get; set; }

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch { }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
