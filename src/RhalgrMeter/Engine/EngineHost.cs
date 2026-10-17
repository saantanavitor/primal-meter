using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using Advanced_Combat_Tracker;
using RhalgrMeter.EngineSetup;

namespace RhalgrMeter.Engine;

/// <summary>
/// Motor embutido: baixa o motor do ACT na primeira vez, carrega e repassa cada linha de log como uma mensagem
/// "LogLine" igual à do IINACT. Todo o trabalho pesado roda fora da thread da interface.
/// </summary>
sealed class EngineHost : IDisposable
{
    public static readonly string LogFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RhalgrMeter");

    readonly string engineFolder = EngineInstaller.DefaultFolder;
    ActEngine? engine;
    Timer? watch;
    volatile bool forwarding;
    string lastStatus = "";

    /// <summary>(estado, detalhe): "setup", "wait", "live" ou "err". Chamado fora da thread da interface.</summary>
    public event Action<string, string>? StatusChanged;
    /// <summary>Mensagem JSON no formato do OverlayPlugin. Chamado fora da thread da interface.</summary>
    public event Action<string>? MessageReceived;

    public void Start()
    {
        forwarding = true;
        lastStatus = "";   // a página voltou para "conectando": reenviar o estado atual
        if (engine != null) { Report(engine.GameProcessId > 0 ? "live" : "wait", ""); return; }
        Task.Run(Boot);
    }

    /// <summary>Para de repassar dados; o motor continua carregado para voltar rápido.</summary>
    public void Pause() => forwarding = false;

    void Boot()
    {
        try
        {
            string version = typeof(EngineHost).Assembly.GetName().Version?.ToString(3) ?? "0";
            EngineInstaller.Ensure(engineFolder, version, msg => Report("setup", msg));
            Report("setup", "Iniciando o motor do ACT…");
            AssemblyLoadContext.Default.Resolving += ResolveFromEngine;
            CreateEngine();
            watch = new Timer(_ => Watch(), null, 1000, 2000);
        }
        catch (Exception ex)
        {
            EngineLog.Write("Falha ao iniciar o motor", ex);
            Report("err", "Não foi possível iniciar o motor do ACT: " + ex.Message);
        }
    }

    // separado para que nenhum tipo do motor seja carregado antes do Resolving estar registrado
    [MethodImpl(MethodImplOptions.NoInlining)]
    void CreateEngine()
    {
        var e = new ActEngine(engineFolder, DeucalionFolder(engineFolder), LogFolder, new EngineLog());
        e.LineRead += OnLine;
        engine = e;
    }

    /// <summary>
    /// O jogo recusa carregar a DLL injetada a partir de %LOCALAPPDATA% (LoadLibraryW dá erro 126, mesmo pelo nome
    /// curto), mas aceita de outras pastas. O Deucalion é copiado para C:\ProgramData\RhalgrMeter\deucalion.
    /// </summary>
    static string DeucalionFolder(string engineFolder)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RhalgrMeter", "deucalion");
        Directory.CreateDirectory(folder);
        foreach (var dll in Directory.GetFiles(engineFolder, "deucalion*.dll"))
        {
            string dst = Path.Combine(folder, Path.GetFileName(dll));
            if (!File.Exists(dst) || !File.ReadAllBytes(dst).AsSpan().SequenceEqual(File.ReadAllBytes(dll)))
                File.Copy(dll, dst, true);
        }
        return folder;
    }

    System.Reflection.Assembly? ResolveFromEngine(AssemblyLoadContext ctx, System.Reflection.AssemblyName name)
    {
        string path = Path.Combine(engineFolder, name.Name + ".dll");
        return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
    }

    void Watch()
    {
        if (engine == null || !forwarding) return;
        if (engine.GameProcessId > 0)
        {
            string? err = ActEngine.InjectionError;
            if (!string.IsNullOrEmpty(err)) Report("err", "O jogo foi encontrado, mas a captura de combate falhou: " + err);
            else Report("live", "");
        }
        else Report("wait", "");
    }

    void OnLine(string line)
    {
        if (!forwarding || string.IsNullOrEmpty(line)) return;
        MessageReceived?.Invoke(JsonSerializer.Serialize(new { type = "LogLine", line = line.Split('|'), rawLine = line }));
    }

    void Report(string status, string detail)
    {
        string key = status + "|" + detail;
        if (key == lastStatus) return;
        lastStatus = key;
        StatusChanged?.Invoke(status, detail);
    }

    public void Dispose()
    {
        watch?.Dispose();
        engine?.Dispose();
    }
}

/// <summary>Erros do motor e do NotACT vão para %APPDATA%\RhalgrMeter\motor.log.</summary>
sealed class EngineLog : IPluginLog
{
    static readonly object gate = new();

    public void Error(Exception ex, string message) => Write(message, ex);
    public void Verbose(Exception ex, string message) => Write(message, ex);

    public static void Write(string message, Exception? ex = null)
    {
        try
        {
            lock (gate)
            {
                Directory.CreateDirectory(Settings.Folder);
                File.AppendAllText(Path.Combine(Settings.Folder, "motor.log"), $"{DateTime.Now:s} {message} {ex}\n");
            }
        }
        catch { }
    }
}
