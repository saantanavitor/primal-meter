// Lê da memória do jogo se o personagem está em combate (o mesmo indicador que o jogo usa na interface).
// Assinatura e cálculo do endereço vêm do OverlayPlugin/IINACT (InCombatMemory73, FFXIVMemory.SigScan, GPL-3);
// aqui a leitura é feita de fora do processo, só com ReadProcessMemory.
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PrimalMeter.Engine;

sealed class GameCombatState : IDisposable
{
    const string Signature = "74??803D??????????74??488B03488BCBFF50";
    const int SignatureOffset = -15, RipOffset = 1;

    int processId;
    IntPtr handle, address;

    /// <summary>true/false se conseguiu ler; null se o jogo não está aberto ou a assinatura não foi achada.</summary>
    public bool? Read(int pid)
    {
        if (pid <= 0) { Close(); return null; }
        if (pid != processId) Attach(pid);
        if (address == IntPtr.Zero) return null;
        var b = new byte[1];
        return ReadProcessMemory(handle, address, b, 1, out _) ? b[0] != 0 : null;
    }

    void Attach(int pid)
    {
        Close();
        processId = pid;
        handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, pid);
        if (handle == IntPtr.Zero) return;
        try
        {
            using var p = Process.GetProcessById(pid);
            address = Scan(p.MainModule!.BaseAddress, p.MainModule.ModuleMemorySize);
        }
        catch { address = IntPtr.Zero; }
    }

    IntPtr Scan(IntPtr start, int size)
    {
        byte?[] pattern = new byte?[Signature.Length / 2];
        for (int i = 0; i < pattern.Length; i++)
        {
            string s = Signature.Substring(i * 2, 2);
            pattern[i] = s == "??" ? null : Convert.ToByte(s, 16);
        }
        const int chunk = 65536;
        var buffer = new byte[chunk + 64];
        long end = start.ToInt64() + size;
        // os blocos se sobrepõem um pouco para não perder uma assinatura que cruze a borda
        for (long at = start.ToInt64(); at < end; at += chunk)
        {
            int toRead = (int)Math.Min(buffer.Length, end - at);
            if (!ReadProcessMemory(handle, new IntPtr(at), buffer, toRead, out var read)) continue;
            int limit = (int)read - pattern.Length - 4;
            for (int i = 0; i < limit; i++)
            {
                int j = 0;
                while (j < pattern.Length && (pattern[j] == null || pattern[j] == buffer[i + j])) j++;
                if (j < pattern.Length) continue;
                int rel = i + pattern.Length + SignatureOffset;
                int disp = BitConverter.ToInt32(buffer, rel);
                return new IntPtr(at + rel + 4 + RipOffset + disp);
            }
        }
        return IntPtr.Zero;
    }

    void Close()
    {
        if (handle != IntPtr.Zero) CloseHandle(handle);
        handle = IntPtr.Zero;
        address = IntPtr.Zero;
        processId = 0;
    }

    public void Dispose() => Close();

    const uint PROCESS_VM_READ = 0x10, PROCESS_QUERY_INFORMATION = 0x400;
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
}
