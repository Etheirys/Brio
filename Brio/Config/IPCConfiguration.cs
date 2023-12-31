namespace Brio.Config;

internal class IPCConfiguration
{
    public bool AllowPenumbraIntegration { get; set; } = true;
    public bool AllowGlamourerIntegration { get; set; } = true;
    public bool AllowWebAPI { get; set; } = false;
}
