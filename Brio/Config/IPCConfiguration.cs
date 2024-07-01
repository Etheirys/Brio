namespace Brio.Config;

internal class IPCConfiguration
{
    public bool EnableBrioIPC { get; set; } = false;
    public bool AllowPenumbraIntegration { get; set; } = false;
    public bool AllowGlamourerIntegration { get; set; } = false;
    public bool AllowMareIntegration { get; set; } = false;
    public bool AllowWebAPI { get; set; } = false;
}
