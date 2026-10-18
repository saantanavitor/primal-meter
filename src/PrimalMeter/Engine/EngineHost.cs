using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using Advanced_Combat_Tracker;
using PrimalMeter.EngineSetup;

namespace PrimalMeter.Engine;

/// <summary>
/// Motor embutido: baixa o motor do ACT na primeira vez, carrega e repassa cada linha de log como uma mensagem
/// "LogLine" igual à do IINACT. Todo o trabalho pesado roda fora da thread da interface.
/// </summary>
sealed class EngineHost : IDisposable
{
    public static readonly string LogFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PrimalMeter");

    readonly string engineFolder = EngineInstaller.DefaultFolder;
    ActEngine? engine;
    Timer? watch, combat, gameState;
    string lastCombatData = "";
    readonly GameCombatState combatMemory = new();
    bool inGame, inAct;
    DateTime? endAt;
    int checking;
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
        lastCombatData = "";
        if (engine != null) { Report(engine.GameProcessId > 0 ? "live" : "wait"); return; }
        Task.Run(Boot);
    }

    /// <summary>Para de repassar dados; o motor continua carregado para voltar rápido.</summary>
    public void Pause() => forwarding = false;

    void Boot()
    {
        try
        {
            string version = typeof(EngineHost).Assembly.GetName().Version?.ToString(3) ?? "0";
            EngineInstaller.Ensure(engineFolder, version, (key, arg) => Report("setup", key, arg));
            Report("setup", "engineStarting");
            AssemblyLoadContext.Default.Resolving += ResolveFromEngine;
            CreateEngine();
            watch = new Timer(_ => Watch(), null, 1000, 2000);
            combat = new Timer(_ => SendCombatData(), null, 1000, 1000);   // como o ACT, atualiza a cada segundo
            gameState = new Timer(_ => CheckCombatState(), null, 1000, 250);
        }
        catch (Exception ex)
        {
            EngineLog.Write("Falha ao iniciar o motor", ex);
            Report("err", "engineFailed", ex.Message);
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
    /// curto), mas aceita de outras pastas. O Deucalion é copiado para C:\ProgramData\PrimalMeter\deucalion.
    /// </summary>
    static string DeucalionFolder(string engineFolder)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PrimalMeter", "deucalion");
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
            if (!string.IsNullOrEmpty(err)) Report("err", "captureFailed", err);
            else Report("live");
        }
        else Report("wait");
    }

    /// <summary>
    /// Como o IINACT: quando o jogo sai de combate, a luta do ACT é encerrada 5 segundos depois (se o combate não
    /// voltar antes). Sem isso, o ACT emendaria todos os pulls numa luta só. Também grava as mudanças na linha 260.
    /// </summary>
    void CheckCombatState()
    {
        if (engine == null || Interlocked.Exchange(ref checking, 1) == 1) return;
        try
        {
            bool game = combatMemory.Read(engine.GameProcessId) ?? false;
            bool act = engine.InActCombat;
            if (game != inGame || act != inAct)
            {
                engine.WriteCombatState(act, game, act != inAct, game != inGame);
                if (game != inGame) endAt = game ? null : DateTime.UtcNow.AddSeconds(5);
                (inGame, inAct) = (game, act);
            }
            if (endAt is DateTime t && DateTime.UtcNow >= t)
            {
                endAt = null;
                engine.EndEncounter();
            }
        }
        catch { }
        finally { checking = 0; }
    }

    /// <summary>Totais do ACT para a interface; só envia quando algo mudou.</summary>
    void SendCombatData()
    {
        if (!forwarding || engine == null) return;
        string? json = engine.BuildCombatData();
        if (json == null || json == lastCombatData) return;
        bool ended = json.Contains("\"isActive\":\"false\"") && !lastCombatData.Contains("\"isActive\":\"false\"");
        lastCombatData = json;
        MessageReceived?.Invoke(json);
        if (ended) SaveFinalTotals(json);
    }

    /// <summary>
    /// Guarda os totais finais de cada luta em %APPDATA%\PrimalMeter\lutas.jsonl (uma linha por luta, as 200 mais
    /// recentes), para conferir números depois ou comparar com o ACT/IINACT.
    /// </summary>
    static void SaveFinalTotals(string json)
    {
        try
        {
            string file = Path.Combine(Settings.Folder, "lutas.jsonl");
            var lines = File.Exists(file) ? File.ReadAllLines(file).ToList() : [];
            lines.Add(DateTime.Now.ToString("s") + "\t" + json);
            if (lines.Count > 200) lines.RemoveRange(0, lines.Count - 200);
            File.WriteAllLines(file, lines);
        }
        catch { }
    }

    void OnLine(string line)
    {
        if (!forwarding || string.IsNullOrEmpty(line)) return;
        MessageReceived?.Invoke(JsonSerializer.Serialize(new { type = "LogLine", line = line.Split('|'), rawLine = line }));
    }

    string lastState = "", lastTextKey = "", lastArg = "";

    /// <summary>Envia o estado com o detalhe já traduzido; <paramref name="textKey"/> é uma chave de <see cref="Text"/>.</summary>
    void Report(string status, string textKey = "", string arg = "")
    {
        string key = status + "|" + textKey + "|" + arg;
        if (key == lastStatus) return;
        lastStatus = key;
        (lastState, lastTextKey, lastArg) = (status, textKey, arg);
        StatusChanged?.Invoke(status, textKey.Length > 0 ? Text.T(textKey, arg) : "");
    }

    /// <summary>Reenvia o último estado (por exemplo, depois de trocar o idioma).</summary>
    public void Resend()
    {
        if (lastState.Length == 0) return;
        lastStatus = "";
        Report(lastState, lastTextKey, lastArg);
    }

    public void Dispose()
    {
        watch?.Dispose();
        combat?.Dispose();
        gameState?.Dispose();
        combatMemory.Dispose();
        // Ao perder o último cliente, o próprio Deucalion se descarrega e restaura o código original do jogo (igual
        // no ACT). Se ele sobrar no jogo (medidor fechado à força), a próxima abertura se reconecta a ele
        // (ver Patcher.MachinaFFXIV), sem desligá-lo: desligar arrancaria ganchos de outras ferramentas.
        engine?.Dispose();
    }
}

/// <summary>Erros do motor e do NotACT vão para %APPDATA%\PrimalMeter\motor.log.</summary>
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
