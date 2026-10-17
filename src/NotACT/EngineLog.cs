// Rhalgr Meter: substitui o IPluginLog do Dalamud, que o NotACT original (IINACT) usava para registrar erros.
namespace Advanced_Combat_Tracker;

public interface IPluginLog
{
    void Error(Exception ex, string message);
    void Verbose(Exception ex, string message);
}
