using System.Drawing;
using System.Windows.Forms;

namespace RhalgrMeter;

/// <summary>Ícone na bandeja do Windows, com o menu do medidor.</summary>
sealed class Tray : IDisposable
{
    readonly NotifyIcon icon;
    readonly ToolStripMenuItem lockItem;

    public Tray(MainWindow w)
    {
        lockItem = new ToolStripMenuItem("Travar medidor", null, (_, _) => w.ToggleLocked()) { ShortcutKeyDisplayString = "Ctrl+Shift+L" };
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Mostrar / ocultar", null, (_, _) => w.ToggleVisible()) { ShortcutKeyDisplayString = "Ctrl+Shift+H" });
        menu.Items.Add(lockItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Reconectar ao IINACT", null, (_, _) => w.TrayCommand("live"));
        menu.Items.Add("Demonstração", null, (_, _) => w.TrayCommand("demo"));
        menu.Items.Add("Reproduzir log…", null, (_, _) => w.TrayCommand("replay"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => w.Close());

        icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Rhalgr Meter",
            ContextMenuStrip = menu,
            Visible = true,
        };
        icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) w.ToggleVisible(); };
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

    public void SetLocked(bool locked) => lockItem.Text = locked ? "Destravar medidor" : "Travar medidor";

    public void Notify(string text) => icon.ShowBalloonTip(5000, "Rhalgr Meter", text, ToolTipIcon.Info);

    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
    }
}
