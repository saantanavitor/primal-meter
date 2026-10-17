namespace RhalgrMeter;

static class AppInfo
{
    /// <summary>
    /// Nome usado em pastas de dados e na trava de instância única. A versão de desenvolvimento (Debug) usa
    /// "RhalgrMeter-dev", para rodar ao lado do medidor instalado sem dividir configurações nem histórico.
    /// Para testar um .exe de versão ao lado do instalado, defina RHALGRMETER_INSTANCE (ex.: "teste").
    /// </summary>
    public static readonly string Id = Environment.GetEnvironmentVariable("RHALGRMETER_INSTANCE") is { Length: > 0 } name
        ? "RhalgrMeter-" + name
#if DEBUG
        : "RhalgrMeter-dev";
#else
        : "RhalgrMeter";
#endif
}
