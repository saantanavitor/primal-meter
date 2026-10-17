using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PrimalMeter;

public partial class App : Application
{
    Mutex? single;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Migration.FromRhalgrMeter();
        Text.Lang = Settings.Load().Language;

        single = new Mutex(true, AppInfo.Id + ".SingleInstance", out bool first);
        if (!first)
        {
            MessageBox.Show(Text.T("alreadyOpen"), "Primal Meter", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        if (AppInfo.Id == "PrimalMeter" && Migration.OldVersionRunning())
        {
            // dois medidores disputariam o Deucalion no jogo
            MessageBox.Show(Text.T("oldRunning"), "Primal Meter", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        DispatcherUnhandledException += OnCrash;
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    void OnCrash(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        string log = Path.Combine(Settings.Folder, "erros.log");
        try { File.AppendAllText(log, $"{DateTime.Now:s} {e.Exception}\n\n"); } catch { }
        MessageBox.Show(Text.T("crash", e.Exception.Message, log), "Primal Meter", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        single?.Dispose();
        base.OnExit(e);
    }
}
