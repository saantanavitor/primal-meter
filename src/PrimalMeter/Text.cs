namespace PrimalMeter;

/// <summary>Textos do lado Windows (bandeja, avisos, estado do motor) em português (padrão) e inglês.</summary>
static class Text
{
    public static string Lang { get; set; } = "pt-BR";
    public static bool English => Lang == "en";

    static readonly Dictionary<string, (string Pt, string En)> strings = new()
    {
        ["alreadyOpen"] = ("O Primal Meter já está aberto. Procure o ícone dele na bandeja, perto do relógio.",
                           "Primal Meter is already running. Look for its icon in the tray, next to the clock."),
        ["oldRunning"] = ("O Rhalgr Meter (o nome antigo do Primal Meter) está aberto. Feche-o pelo ícone na bandeja e abra o Primal Meter de novo.",
                          "Rhalgr Meter (Primal Meter's old name) is running. Close it from its tray icon and open Primal Meter again."),
        ["crash"] = ("Algo deu errado: {0}\n\nOs detalhes ficaram em {1}", "Something went wrong: {0}\n\nDetails were saved to {1}"),
        ["webview2"] = ("O Primal Meter precisa do Microsoft Edge WebView2 Runtime, que já vem no Windows 11.\n\nBaixe em https://developer.microsoft.com/microsoft-edge/webview2/ e abra o Primal Meter de novo.",
                        "Primal Meter needs the Microsoft Edge WebView2 Runtime, which comes with Windows 11.\n\nDownload it from https://developer.microsoft.com/microsoft-edge/webview2/ and open Primal Meter again."),
        ["hotkeysTaken"] = ("Outro programa já usa {0} ou {1}. Escolha outro atalho no menu ⋯ do medidor, ou use o ícone da bandeja para travar e ocultar.",
                            "Another program already uses {0} or {1}. Pick another shortcut in the meter's ⋯ menu, or use the tray icon to lock and hide."),
        ["openLogTitle"] = ("Escolha um log do Primal Meter, do IINACT ou do ACT", "Choose a Primal Meter, IINACT or ACT log"),
        ["openLogFilter"] = ("Logs de combate (*.log)|*.log|Todos os arquivos (*.*)|*.*", "Combat logs (*.log)|*.log|All files (*.*)|*.*"),
        ["readFail"] = ("Não deu para ler esse arquivo: {0}", "Could not read that file: {0}"),

        ["trayShowHide"] = ("Mostrar / ocultar", "Show / hide"),
        ["trayLock"] = ("Travar medidor", "Lock meter"),
        ["trayUnlock"] = ("Destravar medidor", "Unlock meter"),
        ["trayLive"] = ("Voltar ao vivo / reconectar", "Back to live / reconnect"),
        ["trayDemo"] = ("Demonstração", "Demo"),
        ["trayReplay"] = ("Reproduzir log…", "Replay log…"),
        ["trayAutostart"] = ("Iniciar com o Windows", "Start with Windows"),
        ["trayQuit"] = ("Sair", "Quit"),

        ["engineChecking"] = ("Procurando a versão mais recente do motor do ACT…", "Checking for the latest ACT parsing engine…"),
        ["engineDownloading"] = ("Baixando o motor do ACT {0}…", "Downloading the ACT parsing engine {0}…"),
        ["enginePreparing"] = ("Preparando o motor…", "Preparing the engine…"),
        ["engineStarting"] = ("Iniciando o motor do ACT…", "Starting the ACT parsing engine…"),
        ["engineFailed"] = ("Não foi possível iniciar o motor do ACT: {0}", "Could not start the ACT parsing engine: {0}"),
        ["captureFailed"] = ("O jogo foi encontrado, mas a captura de combate falhou: {0}", "The game was found, but combat capture failed: {0}"),
    };

    public static string T(string key, params object[] args)
    {
        var (pt, en) = strings[key];
        string s = English ? en : pt;
        return args.Length > 0 ? string.Format(s, args) : s;
    }
}
