namespace Brio.Config;

public class AppearanceConfiguration
{
    public ApplyNPCHack ApplyNPCHack { get; set; } = ApplyNPCHack.InGPose;

    public bool EnableTinting { get; set; } = true;

    public bool EnableBrioStyle { get; set; } = true;

    public bool EnableBrioColor { get; set; } = true;
    public bool EnableBrioScale { get; set; } = false;
}
