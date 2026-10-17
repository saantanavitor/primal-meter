namespace RhalgrMeter;

static class AppInfo
{
    /// <summary>
    /// Nome usado em pastas de dados e na trava de instância única. A versão de desenvolvimento (Debug) usa
    /// "RhalgrMeter-dev", para rodar ao lado do medidor instalado sem dividir configurações nem histórico.
    /// </summary>
#if DEBUG
    public const string Id = "RhalgrMeter-dev";
#else
    public const string Id = "RhalgrMeter";
#endif
}
