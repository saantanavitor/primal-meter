using System.IO.Compression;
using System.Text.Json;

namespace RhalgrMeter.EngineSetup;

/// <summary>
/// Baixa o FFXIV_ACT_Plugin (motor de parse do ACT, de Ravahn) da página oficial de releases, extrai e corrige
/// para rodar dentro do Rhalgr Meter. O arquivo nunca é redistribuído: cada pessoa baixa na própria máquina.
/// </summary>
public static class EngineInstaller
{
    /// <summary>Muda quando as correções mudam, para reinstalar o motor já baixado.</summary>
    public const string PatchMarker = "RHALGR_ENGINE_PATCH_V1";
    const string ReleasesApi = "https://api.github.com/repos/ravahn/FFXIV_ACT_Plugin/releases/latest";

    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhalgrMeter", "engine");

    /// <summary>
    /// Garante o motor instalado e atualizado em <paramref name="folder"/>. Sem internet, usa o que já estiver lá.
    /// Devolve a versão instalada.
    /// </summary>
    public static Version Ensure(string folder, string versionLabel, Action<string> status)
    {
        Directory.CreateDirectory(folder);
        string pluginPath = Path.Combine(folder, "FFXIV_ACT_Plugin.dll");
        Version? installed = InstalledVersion(pluginPath);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RhalgrMeter/" + versionLabel);

        (Version version, string url)? latest = null;
        try
        {
            status("Procurando a versão mais recente do motor do ACT…");
            latest = LatestRelease(http);
        }
        catch when (installed != null)
        {
            return installed;   // sem internet: segue com o que tem
        }

        if (installed != null && latest!.Value.version <= installed)
            return installed;

        status($"Baixando o motor do ACT {latest!.Value.version}…");
        string zip = Path.Combine(folder, "engine.zip");
        using (var src = http.GetStreamAsync(latest.Value.url).GetAwaiter().GetResult())
        using (var dst = File.Create(zip))
            src.CopyTo(dst);

        status("Preparando o motor…");
        foreach (var f in Directory.GetFiles(folder, "*.dll")) File.Delete(f);
        ZipFile.ExtractToDirectory(zip, folder, true);
        File.Delete(zip);

        // o ACT renomeia o Deucalion distribuído antes de usar; aqui fazemos o mesmo
        foreach (var distrib in Directory.GetFiles(folder, "deucalion*.distrib.dll"))
            File.Move(distrib, distrib.Replace(".distrib.dll", ".dll", StringComparison.OrdinalIgnoreCase), true);

        var patcher = new Patcher(folder, versionLabel);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
        return latest.Value.version;
    }

    static Version? InstalledVersion(string pluginPath)
    {
        if (!File.Exists(pluginPath)) return null;
        try
        {
            using var plugin = new TargetAssembly(pluginPath);
            return plugin.ApiVersionMatches() ? plugin.Version : null;
        }
        catch { return null; }
    }

    static (Version, string) LatestRelease(HttpClient http)
    {
        using var doc = JsonDocument.Parse(http.GetStringAsync(ReleasesApi).GetAwaiter().GetResult());
        var root = doc.RootElement;
        var version = new Version(root.GetProperty("tag_name").GetString()!.TrimStart('v'));
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return (version, asset.GetProperty("browser_download_url").GetString()!);
        }
        throw new InvalidOperationException("A release do FFXIV_ACT_Plugin não tem o arquivo .zip esperado.");
    }
}
