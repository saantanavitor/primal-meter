using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace PrimalMeter;

public partial class MainWindow : Window
{
    // endereço interno da interface; ficou com o nome antigo do projeto (Rhalgr Meter) porque o histórico de lutas
    // fica guardado no navegador embutido por endereço, e trocá-lo apagaria o histórico de quem atualizar
    const string AppOrigin = "https://app.rhalgr";
    const int HotkeyLock = 1, HotkeyShow = 2;

    readonly Settings settings = Settings.Load();
    readonly IinactClient client = new();
    readonly Engine.EngineHost engineHost = new();
    readonly ConcurrentQueue<string> inbox = new();
    readonly DispatcherTimer flush = new() { Interval = TimeSpan.FromMilliseconds(100) };
    readonly DispatcherTimer pointer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(8) };
    CoreWebView2Environment? env;
    HwndSource? source;
    IntPtr hwnd;
    Tray? tray;
    bool locked, pageReady;

    // arrastar / redimensionar
    enum Gesture { None, Move, Resize }
    Gesture gesture;
    Point gestureStart;
    Rect gestureRect;

    public MainWindow()
    {
        InitializeComponent();
        Width = settings.Width;
        Height = settings.Height;
        if (settings.Left is double l && settings.Top is double t && OnScreen(l, t))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = l; Top = t;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = SystemParameters.WorkArea.Right - Width - 40;
            Top = SystemParameters.WorkArea.Top + 120;
        }

        SourceInitialized += OnSourceInitialized;
        Loaded += async (_, _) => await InitWebAsync();
        Closing += (_, _) => SaveWindow();
        Closed += (_, _) => Cleanup();

        client.StatusChanged += st => Dispatcher.InvokeAsync(() =>
        {
            if (settings.DataSource == "iinact") Send(new { host = "status", value = st, detail = "" });
        });
        client.MessageReceived += msg => inbox.Enqueue(msg);
        engineHost.StatusChanged += (st, detail) => Dispatcher.InvokeAsync(() =>
        {
            if (settings.DataSource == "builtin") Send(new { host = "status", value = st, detail });
        });
        engineHost.MessageReceived += msg => inbox.Enqueue(msg);
        flush.Tick += (_, _) => FlushInbox();
        flush.Start();
        pointer.Tick += (_, _) => TrackPointer();
    }

    static bool OnScreen(double left, double top) =>
        left > SystemParameters.VirtualScreenLeft - 100 && top > SystemParameters.VirtualScreenTop - 20 &&
        left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 60 &&
        top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60;

    void OnSourceInitialized(object? sender, EventArgs e)
    {
        hwnd = new WindowInteropHelper(this).Handle;
        source = HwndSource.FromHwnd(hwnd);
        source.CompositionTarget.BackgroundColor = Colors.Transparent;
        source.AddHook(WndProc);
        Native.EnablePerPixelAlpha(hwnd);
        Native.SetExStyle(hwnd, Native.WS_EX_TOOLWINDOW, 0);   // fora do Alt+Tab

        tray = new Tray(this);
        var (okLock, okShow) = RegisterHotkeys();
        if (!okLock || !okShow)
            tray.Notify(Text.T("hotkeysTaken", LockHotkey.Text, ShowHotkey.Text));
        SetLocked(settings.Locked);
    }

    Hotkey LockHotkey => Hotkey.Parse(settings.LockHotkey) ?? Hotkey.Parse(Hotkey.DefaultLock)!.Value;
    Hotkey ShowHotkey => Hotkey.Parse(settings.ShowHotkey) ?? Hotkey.Parse(Hotkey.DefaultShow)!.Value;
    bool lockHotkeyOk, showHotkeyOk;

    /// <summary>(Re)registra os dois atalhos globais. Devolve se cada um foi aceito pelo Windows.</summary>
    (bool, bool) RegisterHotkeys()
    {
        Native.UnregisterHotKey(hwnd, HotkeyLock);
        Native.UnregisterHotKey(hwnd, HotkeyShow);
        var l = LockHotkey;
        var s = ShowHotkey;
        lockHotkeyOk = Native.RegisterHotKey(hwnd, HotkeyLock, l.Modifiers | Native.MOD_NOREPEAT, l.Key);
        showHotkeyOk = Native.RegisterHotKey(hwnd, HotkeyShow, s.Modifiers | Native.MOD_NOREPEAT, s.Key);
        tray?.SetHotkeys(l.Text, s.Text);
        SendHotkeys();
        return (lockHotkeyOk, showHotkeyOk);
    }

    void SendHotkeys() => Send(new { host = "hotkeys", @lock = LockHotkey.Text, show = ShowHotkey.Text, lockOk = lockHotkeyOk, showOk = showHotkeyOk });

    /// <summary>Troca um atalho pedido pela página ("lock" ou "show"); se o Windows recusar, volta ao anterior.</summary>
    void ChangeHotkey(string which, string combo)
    {
        if (Hotkey.Parse(combo) is not Hotkey h) { SendHotkeys(); return; }
        string old = which == "lock" ? settings.LockHotkey : settings.ShowHotkey;
        if (which == "lock") settings.LockHotkey = h.Text; else settings.ShowHotkey = h.Text;
        var (okLock, okShow) = RegisterHotkeys();
        if (!(which == "lock" ? okLock : okShow))
        {
            if (which == "lock") settings.LockHotkey = old; else settings.ShowHotkey = old;
            RegisterHotkeys();
            Send(new { host = "hotkeyTaken", which, combo = h.Text });
            return;
        }
        settings.Save();
    }

    IntPtr WndProc(IntPtr h, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Native.WM_STYLECHANGING && wParam.ToInt64() == Native.GWL_EXSTYLE)
        {
            // o WPF tira WS_EX_LAYERED de janelas sem AllowsTransparency; aqui ele é mantido para o modo travado
            var ss = Marshal.PtrToStructure<Native.STYLESTRUCT>(lParam);
            const long clickThrough = Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT;
            ss.styleNew = locked ? ss.styleNew | (uint)clickThrough : ss.styleNew & ~(uint)clickThrough;
            Marshal.StructureToPtr(ss, lParam, false);
            handled = true;
        }
        else if (msg == Native.WM_HOTKEY)
        {
            if (wParam.ToInt32() == HotkeyLock) ToggleLocked();
            else if (wParam.ToInt32() == HotkeyShow) ToggleVisible();
            handled = true;
        }
        return IntPtr.Zero;
    }

    // ================= WebView2 =================

    async Task InitWebAsync()
    {
        string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppInfo.Id, "WebView2");
        try
        {
            env = await CoreWebView2Environment.CreateAsync(null, dataDir);
            Web.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            await Web.EnsureCoreWebView2Async(env);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(Text.T("webview2"), "Primal Meter", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        var core = Web.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
#if DEBUG
        core.Settings.AreDevToolsEnabled = true;
#else
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
        core.AddWebResourceRequestedFilter(AppOrigin + "/*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += ServeEmbedded;
        core.WebMessageReceived += OnWebMessage;
        core.Navigate(AppOrigin + "/index.html");
    }

    /// <summary>Entrega os arquivos da pasta web/, que estão embutidos no .exe.</summary>
    void ServeEmbedded(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        string path = new Uri(e.Request.Uri).AbsolutePath.TrimStart('/');
        if (path.Length == 0) path = "index.html";
        var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("web/" + path);
        if (stream == null)
        {
            e.Response = env!.CreateWebResourceResponse(null, 404, "Not Found", "");
            return;
        }
        string mime = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            _ => "application/octet-stream",
        };
        e.Response = env!.CreateWebResourceResponse(stream, 200, "OK", $"Content-Type: {mime}\nCache-Control: no-store");
    }

    void Send(object message)
    {
        if (!pageReady || Web.CoreWebView2 == null) return;
        Web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message));
    }

    /// <summary>Junta as mensagens do IINACT e entrega à página em lotes, para não travar em lutas grandes.</summary>
    void FlushInbox()
    {
        if (!pageReady || inbox.IsEmpty) return;
        var items = new List<string>();
        while (items.Count < 2000 && inbox.TryDequeue(out var m)) items.Add(m);
        Send(new { host = "batch", items });
    }

    void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        JsonElement m;
        try { m = JsonDocument.Parse(e.WebMessageAsJson).RootElement; } catch { return; }
        if (!m.TryGetProperty("cmd", out var c)) return;
        switch (c.GetString())
        {
            case "ready":
                pageReady = true;
                Send(new { host = "lang", value = settings.Language });
                Send(new { host = "locked", value = locked });
                SendHotkeys();
                if (m.TryGetProperty("url", out var u) && u.GetString() is string url && url.Length > 0) settings.WsUrl = url;
                StartSource();
                break;
            case "connect":
                if (m.TryGetProperty("url", out var u2) && u2.GetString() is string url2 && url2.Length > 0) settings.WsUrl = url2;
                if (m.TryGetProperty("source", out var s) && s.GetString() is "builtin" or "iinact") settings.DataSource = s.GetString()!;
                StartSource();
                break;
            case "disconnect":
                client.Stop();
                engineHost.Pause();
                while (inbox.TryDequeue(out _)) { }
                break;
            case "drag": BeginGesture(Gesture.Move); break;
            case "resize": BeginGesture(Gesture.Resize); break;
            case "lock": SetLocked(m.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.True); break;
            case "hide": Hide(); break;
            case "quit": Close(); break;
            case "openLog": _ = OpenLogAsync(); break;
            case "hotkeyCapture":
                // enquanto a página espera o novo atalho, os atuais ficam desligados; senão apertar o atalho de
                // sempre travaria o medidor em vez de chegar à página
                if (m.TryGetProperty("value", out var cap) && cap.ValueKind == JsonValueKind.True)
                {
                    Native.UnregisterHotKey(hwnd, HotkeyLock);
                    Native.UnregisterHotKey(hwnd, HotkeyShow);
                }
                else RegisterHotkeys();
                break;
            case "hotkey":
                if (m.TryGetProperty("which", out var w) && w.GetString() is "lock" or "show" &&
                    m.TryGetProperty("combo", out var cb) && cb.GetString() is string combo)
                    ChangeHotkey(w.GetString()!, combo);
                break;
            case "lang":
                if (m.TryGetProperty("value", out var l) && l.GetString() is "pt-BR" or "en")
                {
                    settings.Language = Text.Lang = l.GetString()!;
                    settings.Save();
                    tray?.RefreshTexts();
                    engineHost.Resend();
                }
                break;
        }
    }

    /// <summary>Liga a fonte de dados escolhida (motor embutido ou IINACT) e desliga a outra.</summary>
    void StartSource()
    {
        while (inbox.TryDequeue(out _)) { }
        Send(new { host = "source", value = settings.DataSource });
        if (settings.DataSource == "iinact")
        {
            engineHost.Pause();
            client.Start(settings.WsUrl);
        }
        else
        {
            client.Stop(silent: true);
            engineHost.Start();
        }
        settings.Save();
    }

    // ================= Mover e redimensionar =================

    Point CursorDip()
    {
        Native.GetCursorPos(out var p);
        return source?.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y)) ?? new Point(p.X, p.Y);
    }

    void BeginGesture(Gesture g)
    {
        if (locked || !Native.LeftButtonDown) return;
        gesture = g;
        gestureStart = CursorDip();
        gestureRect = new Rect(Left, Top, Width, Height);
        pointer.Start();
    }

    void TrackPointer()
    {
        if (gesture == Gesture.None || !Native.LeftButtonDown)
        {
            gesture = Gesture.None;
            pointer.Stop();
            SaveWindow();
            return;
        }
        var p = CursorDip();
        double dx = p.X - gestureStart.X, dy = p.Y - gestureStart.Y;
        if (gesture == Gesture.Move)
        {
            Left = gestureRect.Left + dx;
            Top = gestureRect.Top + dy;
        }
        else
        {
            Width = Math.Max(MinWidth, gestureRect.Width + dx);
            Height = Math.Max(MinHeight, gestureRect.Height + dy);
        }
    }

    // ================= Travar, mostrar, sair =================

    public void SetLocked(bool value)
    {
        locked = value;
        settings.Locked = value;
        if (hwnd != IntPtr.Zero)
        {
            if (value)
            {
                // camada + transparente: o Windows entrega todos os cliques para a janela de baixo (o jogo)
                Native.SetExStyle(hwnd, Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT, 0);
                Native.SetLayeredWindowAttributes(hwnd, 0, 255, Native.LWA_ALPHA);
            }
            else
            {
                Native.SetExStyle(hwnd, 0, Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT);
            }
        }
        tray?.SetLocked(value);
        Send(new { host = "locked", value });
    }

    public void ToggleLocked()
    {
        SetLocked(!locked);
        if (!IsVisible) ShowMeter();
    }

    public void ToggleVisible()
    {
        if (IsVisible) Hide(); else ShowMeter();
    }

    public void ShowMeter()
    {
        Show();
        Topmost = false;
        Topmost = true;
    }

    public void TrayCommand(string action)
    {
        ShowMeter();
        if (action == "replay") _ = OpenLogAsync();
        else Send(new { host = "menu", action });
    }

    async Task OpenLogAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = Text.T("openLogTitle"),
            Filter = Text.T("openLogFilter"),
        };
        string iinact = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IINACT");
        if (Directory.Exists(Engine.EngineHost.LogFolder)) dlg.InitialDirectory = Engine.EngineHost.LogFolder;
        else if (Directory.Exists(iinact)) dlg.InitialDirectory = iinact;
        if (dlg.ShowDialog(this) != true) return;

        string path = dlg.FileName;
        try
        {
            // o IINACT pode estar escrevendo no arquivo agora; lê compartilhado e guarda só as linhas que o medidor usa
            string text = await Task.Run(() => ReadUsefulLines(path));
            Send(new { host = "replay", name = Path.GetFileName(path), text });
        }
        catch (Exception ex)
        {
            MessageBox.Show(Text.T("readFail", ex.Message), "Primal Meter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    static readonly string[] UsefulPrefixes = ["01|", "02|", "03|", "21|", "22|", "24|", "25|", "26|", "260|"];

    static string ReadUsefulLines(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(fs, Encoding.UTF8);
        var sb = new StringBuilder();
        // das linhas 261 (261|data|Add|...) só interessam Add e Change, que trazem nome, job e dono dos pets
        while (reader.ReadLine() is string line)
        {
            int bar = line.IndexOf('|');
            if (bar <= 0) continue;
            string type = line[..(bar + 1)];
            if (type == "261|")
            {
                int second = line.IndexOf('|', bar + 1);
                if (second < 0) continue;
                string rest = line[(second + 1)..];
                if (!rest.StartsWith("Add|") && !rest.StartsWith("Change|")) continue;
            }
            else if (Array.IndexOf(UsefulPrefixes, type) < 0) continue;
            sb.Append(line).Append('\n');
        }
        return sb.ToString();
    }

    void SaveWindow()
    {
        if (WindowState != WindowState.Normal) return;
        settings.Left = Left;
        settings.Top = Top;
        settings.Width = Width;
        settings.Height = Height;
        settings.Save();
    }

    void Cleanup()
    {
        flush.Stop();
        pointer.Stop();
        client.Dispose();
        engineHost.Dispose();
        if (hwnd != IntPtr.Zero)
        {
            Native.UnregisterHotKey(hwnd, HotkeyLock);
            Native.UnregisterHotKey(hwnd, HotkeyShow);
        }
        tray?.Dispose();
        settings.Save();
    }
}
