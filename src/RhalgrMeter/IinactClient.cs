using System.IO;
using System.Net.WebSockets;
using System.Text;

namespace RhalgrMeter;

/// <summary>
/// Conecta no servidor WebSocket do IINACT (ou do OverlayPlugin do ACT), assina os eventos de combate
/// e repassa cada mensagem como texto. Reconecta sozinho a cada 3 segundos.
/// </summary>
public sealed class IinactClient : IDisposable
{
    const string Subscribe = "{\"call\":\"subscribe\",\"events\":[\"CombatData\",\"LogLine\",\"ChangePrimaryPlayer\"]}";

    CancellationTokenSource? cts;

    /// <summary>"wait", "live", "err" ou "idle". Chamado fora da thread da interface.</summary>
    public event Action<string>? StatusChanged;
    /// <summary>Uma mensagem JSON do IINACT. Chamado fora da thread da interface.</summary>
    public event Action<string>? MessageReceived;

    public void Start(string url)
    {
        Stop(silent: true);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "ws" && uri.Scheme != "wss"))
        {
            StatusChanged?.Invoke("err");
            return;
        }
        var source = cts = new CancellationTokenSource();
        _ = Task.Run(() => Loop(uri, source.Token));
    }

    public void Stop(bool silent = false)
    {
        cts?.Cancel();
        cts = null;
        if (!silent) StatusChanged?.Invoke("idle");
    }

    async Task Loop(Uri uri, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        while (!ct.IsCancellationRequested)
        {
            StatusChanged?.Invoke("wait");
            using (var ws = new ClientWebSocket())
            {
                try
                {
                    using (var connect = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        connect.CancelAfter(TimeSpan.FromSeconds(5));
                        await ws.ConnectAsync(uri, connect.Token);
                    }
                    StatusChanged?.Invoke("live");
                    await ws.SendAsync(Encoding.UTF8.GetBytes(Subscribe), WebSocketMessageType.Text, true, ct);

                    using var message = new MemoryStream();
                    while (ws.State == WebSocketState.Open)
                    {
                        var r = await ws.ReceiveAsync(buffer, ct);
                        if (r.MessageType == WebSocketMessageType.Close) break;
                        message.Write(buffer, 0, r.Count);
                        if (!r.EndOfMessage) continue;
                        MessageReceived?.Invoke(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
                        message.SetLength(0);
                    }
                }
                catch when (ct.IsCancellationRequested) { return; }
                catch { /* IINACT fechado ou ainda não iniciado: tenta de novo */ }
            }
            if (ct.IsCancellationRequested) return;
            StatusChanged?.Invoke("err");
            try { await Task.Delay(3000, ct); } catch { return; }
        }
    }

    public void Dispose() => Stop(silent: true);
}
