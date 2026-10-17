using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace RhalgrMeter;

public partial class App : Application
{
    Mutex? single;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        single = new Mutex(true, "RhalgrMeter.SingleInstance", out bool first);
        if (!first)
        {
            MessageBox.Show("O Rhalgr Meter já está aberto. Procure o ícone dele na bandeja, perto do relógio.",
                "Rhalgr Meter", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        DispatcherUnhandledException += OnCrash;
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    void OnCrash(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try { File.AppendAllText(Path.Combine(Settings.Folder, "erros.log"), $"{DateTime.Now:s} {e.Exception}\n\n"); } catch { }
        MessageBox.Show("Algo deu errado: " + e.Exception.Message + "\n\nOs detalhes ficaram em " + Path.Combine(Settings.Folder, "erros.log"),
            "Rhalgr Meter", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        single?.Dispose();
        base.OnExit(e);
    }
}
