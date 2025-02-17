namespace Brio.Config;

public class IPCConfiguration
{
    public bool EnableBrioIPC { get; set; } = true;
    public bool AllowPenumbraIntegration { get; set; } = true;
    public bool AllowGlamourerIntegration { get; set; } = true;
    public bool AllowMareIntegration { get; set; } = true;
    public bool AllowWebAPI { get; set; } = false;
    public bool AllowCustomizePlusIntegration { get; set; } = true;
}
