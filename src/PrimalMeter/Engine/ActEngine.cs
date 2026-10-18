// Hospeda o FFXIV_ACT_Plugin (motor de parse do ACT) fora do jogo, dentro do Primal Meter.
// Baseado no FfxivActPluginWrapper do IINACT (https://github.com/marzent/IINACT, GPL-3), adaptado para o modo
// "de fora": o próprio motor acha o processo do jogo, lê a memória por fora e captura a rede com o Deucalion.
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using FFXIV_ACT_Plugin.Parse;
using FFXIV_ACT_Plugin.Resource;
using Machina.FFXIV;
using Machina.FFXIV.Deucalion;
using Machina.FFXIV.Headers.Opcodes;
using Microsoft.MinIoC;

namespace PrimalMeter.Engine;

sealed class ActEngine : IDisposable
{
    readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin plugin;
    readonly ParseMediator parseMediator;
    readonly LogOutput logOutput;
    readonly IProcessManager processManager;

    /// <summary>Linha de log no formato de rede do ACT ("21|data|..."). Chamado na thread de leitura do NotACT.</summary>
    public event Action<string>? LineRead;

    public ActEngine(string engineFolder, string deucalionFolder, string logFolder, IPluginLog log)
    {
        Directory.CreateDirectory(logFolder);
        ActGlobals.Init();
        ActGlobals.oFormActMain = new FormActMain(log) { LogFilePath = logFolder };

        plugin = new FFXIV_ACT_Plugin.FFXIV_ACT_Plugin();
        plugin.ConfigureIOC();
        var ioc = plugin._iocContainer;
        ioc.Resolve<ResourceManager>().LoadResources();

        var subscription = ioc.Resolve<DataSubscription>();
        SetProperty(plugin, "DataSubscription", subscription);
        parseMediator = ioc.Resolve<ParseMediator>();
        plugin._dataCollection = ioc.Resolve<DataCollection>();

        logOutput = (LogOutput)plugin._dataCollection._logOutput;
        var logFormat = plugin._dataCollection._logFormat;
        processManager = ((ScanMemory)plugin._dataCollection._scanMemory)._processManager;

        // ACTWrapper: a ponte do motor com o "ACT" (aqui, o NotACT)
        var actWrapper = (FFXIV_ACT_Plugin.Common.ACTWrapper)logOutput._actWrapper;
        actWrapper.TimeStampLen = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture).Length + 3;
        actWrapper.LogPathHasCharName = false;
        actWrapper.OverrideMainFormVisible = true;
        ACT_UIMods.UpdateACTTables(false);
        ActGlobals.oFormActMain.FfxivPlugin = plugin;
        subscription.ZoneChanged += (_, zoneName) => ActGlobals.oFormActMain.ChangeZone(zoneName);

        // configurações: processo 0 = o motor procura o jogo sozinho
        var settings = plugin._dataCollection._settingsMediator;
        var collection = new DataCollectionSettingsEventArgs
        {
            LogFileFolder = logFolder,
            RegionID = Region.Global,
            ProcessID = 0,
        };
        settings.DataCollectionSettings = collection;
        var parse = new ParseSettings
        {
            DisableDamageShield = false,
            DisableCombinePets = false,
            LanguageID = Language.English,
            ParseFilter = ParseFilterMode.None,
            SimulateIndividualDoTCrits = false,
            ShowRealDoTTicks = false,
            ShowDebug = false,
            EnableBenchmarks = false,
        };
        settings.ParseSettings = parse;
        settings.ProcessException = (_, text) => log.Verbose(new Exception(text), "[FFXIV_ACT_Plugin]");

        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue,
            logFormat.FormatParseSettings(parse.DisableDamageShield, parse.DisableCombinePets, parse.LanguageID,
                parse.ParseFilter, parse.SimulateIndividualDoTCrits, parse.ShowRealDoTTicks));
        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue,
            logFormat.FormatMemorySettings(collection.ProcessID, collection.LogFileFolder, collection.LogAllNetworkData,
                collection.DisableCombatLog, collection.RegionID));
        logOutput.ConfigureLogFile();
        ActGlobals.oFormActMain.GetDateTimeFromLog = parseMediator.ParseLogDateTime;

        SetProperty(plugin, "DataRepository", ioc.Resolve<IDataRepository>());

        OpcodeManager.Instance.SetRegion(GameRegion.Global);
        DeucalionInjector.DeucalionPath = deucalionFolder;

        ActGlobals.oFormActMain.BeforeLogLineRead += OnBeforeLogLineRead;
        plugin._dataCollection.StartMemory();
    }

    void OnBeforeLogLineRead(bool isImport, LogLineEventArgs info)
    {
        try { LineRead?.Invoke(info.logLine); } catch { }
        // wipe: o jogo manda o comando de reinício da instância (linha 33, 4000000F); o IINACT encerra a luta aqui
        if (info.logLine.StartsWith("33|") && info.logLine.Split('|') is { Length: > 3 } f && f[3] is "4000000F" or "40000010")
            EndEncounter();
        (info.logLine, info.detectedType) = parseMediator.BeforeLogLineRead(isImport, info.detectedTime, info.logLine);
    }

    // campos que a interface usa (web/index.html, onCombat); ler todos custaria muito a cada segundo
    static readonly string[] EncounterKeys = ["title", "duration", "DURATION", "encdps", "enchps", "CurrentZoneName"];
    static readonly string[] CombatantKeys = ["name", "Job", "duration", "encdps", "dps", "damage", "damage%", "crithit%", "DirectHitPct", "maxhit",
        "enchps", "healed", "healed%", "OverHealPct", "maxheal", "damagetaken", "deaths"];

    /// <summary>
    /// Totais da luta atual calculados pelo próprio ACT (NotACT): escudos como cura, Limit Break separado, DoTs
    /// estimados e as regras de início e fim de luta do ACT. Mesmo formato do evento CombatData do OverlayPlugin
    /// (baseado no MiniParseEventSource do IINACT). Devolve null se ainda não há luta.
    /// </summary>
    public string? BuildCombatData()
    {
        var encounter = ActGlobals.oFormActMain?.ActiveZone?.ActiveEncounter;
        if (encounter == null || EncounterData.ExportVariables.Count == 0 || CombatantData.ExportVariables.Count == 0)
            return null;
        try
        {
            var allies = encounter.GetAllies();
            var enc = new Dictionary<string, string>();
            foreach (var key in EncounterKeys)
                if (EncounterData.ExportVariables.TryGetValue(key, out var f))
                    try { enc[key] = f.GetExportString(encounter, allies, ""); } catch { }

            var combatants = new Dictionary<string, Dictionary<string, string>>();
            foreach (var ally in allies)
            {
                var values = new Dictionary<string, string>();
                foreach (var key in CombatantKeys)
                    if (CombatantData.ExportVariables.TryGetValue(key, out var f))
                        try { values[key] = f.GetExportString(ally, ""); } catch { }
                combatants[ally.Name] = values;
            }
            return JsonSerializer.Serialize(new
            {
                type = "CombatData",
                Encounter = enc,
                Combatant = combatants,
                isActive = encounter.Active ? "true" : "false",
            });
        }
        catch
        {
            return null;   // a luta mudou no meio da leitura; o próximo segundo tenta de novo
        }
    }

    /// <summary>Se o ACT (NotACT) considera que há uma luta em andamento.</summary>
    public bool InActCombat => ActGlobals.oFormActMain?.InCombat == true;

    /// <summary>Encerra a luta atual, como o comando "end" do ACT.</summary>
    public void EndEncounter()
    {
        try { if (InActCombat) ActGlobals.oFormActMain.EndCombat(true); } catch { }
    }

    /// <summary>
    /// Grava a linha 260 (estado de combate do ACT e do jogo), no mesmo formato do OverlayPlugin/IINACT. Ela também
    /// separa as lutas quando o log é reproduzido depois.
    /// </summary>
    public void WriteCombatState(bool inAct, bool inGame, bool actChanged, bool gameChanged)
    {
        static string B(bool v) => v ? "1" : "0";
        try { logOutput.WriteLine((LogMessageType)260, DateTime.MinValue, $"{B(inAct)}|{B(inGame)}|{B(actChanged)}|{B(gameChanged)}"); } catch { }
    }

    /// <summary>Id do processo do jogo que o motor está lendo, ou 0.</summary>
    public int GameProcessId => processManager.Current?.ProcessId ?? 0;

    /// <summary>Último erro ao injetar o Deucalion no jogo, se houve.</summary>
    public static string? InjectionError => DeucalionInjector.LastInjectionError;

    static void SetProperty(object obj, string name, object value) =>
        obj.GetType().InvokeMember(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
            null, obj, [value]);

    public void Dispose()
    {
        try
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= OnBeforeLogLineRead;
            plugin.DeInitPlugin();
            plugin.Dispose();
            ActGlobals.Dispose();
        }
        catch { }
    }
}
