namespace PrimalMeter;

/// <summary>Atalho global no formato de texto "Ctrl+Alt+L" (modificadores + uma tecla A–Z, 0–9 ou F1–F12).</summary>
readonly record struct Hotkey(uint Modifiers, uint Key, string Text)
{
    public const string DefaultLock = "Ctrl+Alt+L";
    public const string DefaultShow = "Ctrl+Alt+H";

    /// <summary>Lê o texto do atalho; precisa de Ctrl ou Alt, para não roubar teclas comuns do jogo.</summary>
    public static Hotkey? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        uint mods = 0, key = 0;
        foreach (string raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string part = raw.ToUpperInvariant();
            switch (part)
            {
                case "CTRL": mods |= Native.MOD_CONTROL; break;
                case "ALT": mods |= Native.MOD_ALT; break;
                case "SHIFT": mods |= Native.MOD_SHIFT; break;
                default:
                    if (key != 0) return null;
                    if (part.Length == 1 && (char.IsAsciiLetterUpper(part[0]) || char.IsAsciiDigit(part[0]))) key = part[0];
                    else if (part.Length is 2 or 3 && part[0] == 'F' && int.TryParse(part[1..], out int f) && f is >= 1 and <= 12) key = (uint)(0x70 + f - 1);
                    else return null;
                    break;
            }
        }
        if (key == 0 || (mods & (Native.MOD_CONTROL | Native.MOD_ALT)) == 0) return null;
        return new Hotkey(mods, key, Format(mods, key));
    }

    static string Format(uint mods, uint key)
    {
        var parts = new List<string>();
        if ((mods & Native.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & Native.MOD_ALT) != 0) parts.Add("Alt");
        if ((mods & Native.MOD_SHIFT) != 0) parts.Add("Shift");
        parts.Add(key >= 0x70 && key <= 0x7B ? "F" + (key - 0x70 + 1) : ((char)key).ToString());
        return string.Join("+", parts);
    }
}
