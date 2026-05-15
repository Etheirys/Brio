namespace Brio.Config;

public class AppearanceConfiguration
{
    public ApplyNPCHack ApplyNPCHack { get; set; } = ApplyNPCHack.InGPose;

    public bool EnableTinting { get; set; } = true;

    public bool EnableBrioStyle { get; set; } = true;

    public bool EnableBrioColor { get; set; } = true;
    public bool EnableBrioScale { get; set; } = false;

    public string Theme { get; set; } = "Brio Dark";
    public float WindowOpacity { get; set; } = 0.86f;
    public bool EnableBlur { get; set; } = true;
}
