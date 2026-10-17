using System.IO;
using System.Text.Json;

namespace PrimalMeter;

/// <summary>Preferências da janela, salvas em %APPDATA%\PrimalMeter\settings.json.</summary>
public sealed class Settings
{
    public static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppInfo.Id);
    static string FilePath => Path.Combine(Folder, "settings.json");

    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 380;
    public double Height { get; set; } = 480;
    public string WsUrl { get; set; } = "ws://127.0.0.1:10501/ws";
    public bool Locked { get; set; }
    /// <summary>"builtin": motor do ACT embutido (sem plugins). "iinact": servidor WebSocket do IINACT.</summary>
    public string DataSource { get; set; } = "builtin";
    /// <summary>"pt-BR" (padrão) ou "en".</summary>
    public string Language { get; set; } = "pt-BR";

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
