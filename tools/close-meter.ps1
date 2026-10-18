# Fecha o Primal Meter do jeito normal (como "Sair" na bandeja), para testes. Uso: powershell -File tools/close-meter.ps1
Add-Type -TypeDefinition @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class CloseMeter {
  public delegate bool CB(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(CB cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
}
'@
foreach ($p in Get-Process PrimalMeter -ErrorAction SilentlyContinue) {
    [CloseMeter]::EnumWindows({ param($h, $l)
        $id = 0; [void][CloseMeter]::GetWindowThreadProcessId($h, [ref]$id)
        $sb = New-Object Text.StringBuilder 100; [void][CloseMeter]::GetWindowText($h, $sb, 100)
        if ($id -eq $p.Id -and $sb.ToString() -eq "Primal Meter") { [void][CloseMeter]::PostMessage($h, 0x10, [IntPtr]::Zero, [IntPtr]::Zero); return $false }
        return $true }, [IntPtr]::Zero) | Out-Null
    $p.WaitForExit(15000) | Out-Null
    "Primal Meter $($p.Id) fechado: $($p.HasExited)"
}
