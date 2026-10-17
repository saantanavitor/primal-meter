namespace PrimalMeter;

static class AppInfo
{
    /// <summary>
    /// Nome usado em pastas de dados e na trava de instância única. A versão de desenvolvimento (Debug) usa
    /// "PrimalMeter-dev", para rodar ao lado do medidor instalado sem dividir configurações nem histórico.
    /// Para testar um .exe de versão ao lado do instalado, defina PRIMALMETER_INSTANCE (ex.: "teste").
    /// </summary>
    public static readonly string Id = Environment.GetEnvironmentVariable("PRIMALMETER_INSTANCE") is { Length: > 0 } name
        ? "PrimalMeter-" + name
#if DEBUG
        : "PrimalMeter-dev";
#else
        : "PrimalMeter";
#endif
}
