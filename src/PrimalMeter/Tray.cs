using System.Drawing;
using System.Windows.Forms;

namespace PrimalMeter;

/// <summary>Ícone na bandeja do Windows, com o menu do medidor.</summary>
sealed class Tray : IDisposable
{
    readonly NotifyIcon icon;
    readonly ToolStripMenuItem showItem, lockItem, liveItem, demoItem, replayItem, quitItem;
    bool locked;

    public Tray(MainWindow w)
    {
        showItem = new ToolStripMenuItem("", null, (_, _) => w.ToggleVisible()) { ShortcutKeyDisplayString = "Ctrl+Shift+H" };
        lockItem = new ToolStripMenuItem("", null, (_, _) => w.ToggleLocked()) { ShortcutKeyDisplayString = "Ctrl+Shift+L" };
        liveItem = new ToolStripMenuItem("", null, (_, _) => w.TrayCommand("live"));
        demoItem = new ToolStripMenuItem("", null, (_, _) => w.TrayCommand("demo"));
        replayItem = new ToolStripMenuItem("", null, (_, _) => w.TrayCommand("replay"));
        quitItem = new ToolStripMenuItem("", null, (_, _) => w.Close());

        var menu = new ContextMenuStrip();
        menu.Items.AddRange([showItem, lockItem, new ToolStripSeparator(), liveItem, demoItem, replayItem, new ToolStripSeparator(), quitItem]);

        icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Primal Meter",
            ContextMenuStrip = menu,
            Visible = true,
        };
        icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) w.ToggleVisible(); };
        RefreshTexts();
    }

    static Icon LoadIcon()
    {
        try
        {
            if (Environment.ProcessPath is string exe && Icon.ExtractAssociatedIcon(exe) is Icon i) return i;
        }
        catch { }
        return SystemIcons.Application;
    }

    /// <summary>Atualiza os textos depois de uma troca de idioma.</summary>
    public void RefreshTexts()
    {
        showItem.Text = Text.T("trayShowHide");
        lockItem.Text = Text.T(locked ? "trayUnlock" : "trayLock");
        liveItem.Text = Text.T("trayLive");
        demoItem.Text = Text.T("trayDemo");
        replayItem.Text = Text.T("trayReplay");
        quitItem.Text = Text.T("trayQuit");
    }

    public void SetLocked(bool value)
    {
        locked = value;
        lockItem.Text = Text.T(locked ? "trayUnlock" : "trayLock");
    }

    public void Notify(string text) => icon.ShowBalloonTip(5000, "Primal Meter", text, ToolTipIcon.Info);

    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
    }
}
