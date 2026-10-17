using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace PrimalMeter.Engine;

/// <summary>
/// O Deucalion continua carregado no jogo depois que o medidor fecha. Numa nova conexão, o gancho de recepção
/// já está instalado ("Detour is already initialized") e o motor do ACT desiste de capturar. Por isso o medidor
/// pede ao Deucalion para se desligar ao fechar e, ao abrir, desliga um que tenha sobrado de uma sessão anterior.
/// </summary>
static class DeucalionPipe
{
    const byte OpcodeExit = 2;

    /// <summary>Desliga o Deucalion carregado em cada processo do jogo, se houver. Devolve quantos desligou.</summary>
    public static int ExitAll()
    {
        int count = 0;
        foreach (var game in Process.GetProcessesByName("ffxiv_dx11"))
        {
            if (Exit(game.Id)) count++;
            game.Dispose();
        }
        return count;
    }

    public static bool Exit(int processId)
    {
        string name = $"deucalion-{processId}";
        try
        {
            using var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut);
            pipe.Connect(500);   // sem Deucalion no jogo, não há pipe e a conexão expira
            // o Deucalion manda um "SERVER HELLO" ao conectar; pipes não aceitam ReadTimeout, daí o Wait
            var hello = new byte[4096];
            pipe.ReadAsync(hello, 0, hello.Length).Wait(2000);
            // cabeçalho do protocolo: tamanho (4 bytes), opcode (1), canal (4)
            pipe.Write([9, 0, 0, 0, OpcodeExit, 0, 0, 0, 0], 0, 9);
            pipe.Flush();
        }
        catch
        {
            return false;
        }
        Thread.Sleep(1500);   // tempo para o Deucalion soltar os ganchos e se descarregar
        return true;
    }
}
