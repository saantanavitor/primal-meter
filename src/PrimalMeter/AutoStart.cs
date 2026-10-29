using Microsoft.Win32;

namespace PrimalMeter;

/// <summary>
/// "Iniciar com o Windows": um valor na chave Run do usuário (não precisa de administrador). O medidor abre com
/// --tray, direto na bandeja, sem mostrar a janela.
/// </summary>
static class AutoStart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string TrayArg = "--tray";

    static string Command => $"\"{Environment.ProcessPath}\" {TrayArg}";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppInfo.Id) is string;
        }
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(AppInfo.Id, Command);
        else key.DeleteValue(AppInfo.Id, throwOnMissingValue: false);
    }

    /// <summary>Se a opção está ligada mas o .exe mudou de pasta, aponta a inicialização para o lugar atual.</summary>
    public static void RefreshPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key?.GetValue(AppInfo.Id) is string current && current != Command)
                key.SetValue(AppInfo.Id, Command);
        }
        catch { }
    }
}
