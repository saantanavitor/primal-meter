using System.Diagnostics;
using System.IO;

namespace PrimalMeter;

/// <summary>
/// O projeto se chamava Rhalgr Meter até a versão 0.2.0. Na primeira execução do Primal Meter, as configurações,
/// o histórico (guardado nos dados do WebView2) e o motor já baixado são copiados das pastas antigas.
/// </summary>
static class Migration
{
    const string OldId = "RhalgrMeter";

    public static bool OldVersionRunning()
    {
        var running = Process.GetProcessesByName(OldId);
        foreach (var p in running) p.Dispose();
        return running.Length > 0;
    }

    public static void FromRhalgrMeter()
    {
        if (AppInfo.Id != "PrimalMeter") return;   // versão de desenvolvimento e instâncias de teste não migram

        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (!File.Exists(Path.Combine(roaming, AppInfo.Id, "settings.json")))
            CopyFolder(Path.Combine(roaming, OldId), Path.Combine(roaming, AppInfo.Id));

        if (!Directory.Exists(Path.Combine(local, AppInfo.Id, "WebView2")))
            CopyFolder(Path.Combine(local, OldId, "WebView2"), Path.Combine(local, AppInfo.Id, "WebView2"));

        if (!Directory.Exists(Path.Combine(local, AppInfo.Id, "engine")))
            CopyFolder(Path.Combine(local, OldId, "engine"), Path.Combine(local, AppInfo.Id, "engine"));
    }

    static void CopyFolder(string from, string to)
    {
        if (!Directory.Exists(from)) return;
        foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            try
            {
                string target = Path.Combine(to, Path.GetRelativePath(from, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: false);
            }
            catch { /* arquivo em uso ou já copiado: segue com os outros */ }
        }
    }
}
